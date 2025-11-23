namespace App.ExternalSorter.Merge.PolyPhase;

using System.Buffers;
using App.FileSystem;
using FileSystem.Interfaces;
using Sorting;

/// <summary>
/// Represents a queue of file paths that can be read as a stream of SortKeys.
/// </summary>
public class Tape : IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly Queue<string> _files;
    private int _dummyRuns;

    private const int BufferSize = 40 * 1024; // 40KB - stays on SOH

    /// <summary>
    /// Initializes a new instance of the <see cref="Tape"/> class.
    /// </summary>
    public Tape(int capacity, IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _files = new Queue<string>(capacity);
    }

    /// <summary>
    /// Adds a file path to the tape queue.
    /// </summary>
    public void Enqueue(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Path cannot be null or whitespace", nameof(filePath));

        _files.Enqueue(filePath);
    }

    /// <summary>
    /// Dequeues the next file path, handling dummy runs.
    /// Returns null if it was a dummy run.
    /// </summary>
    public string? DequeuePathOnly()
    {
        if (_dummyRuns > 0)
        {
            _dummyRuns--;
            return null;
        }

        return _files.TryDequeue(out var path) ? path : null;
    }

    /// <summary>
    /// Gets the path of the current file at the head of the tape, without dequeuing it.
    /// </summary>
    /// <returns>The file path, or null if the tape is empty or the head is a dummy run.</returns>
    public string? GetCurrentFilePath()
    {
        // Skip dummy runs to find the first real file
        if (_dummyRuns >= _files.Count + _dummyRuns)
            return null; // Only dummy runs left
            
        return _files.TryPeek(out var path) ? path : null;
    }

    /// <summary>
    /// Adds dummy runs to the tape.
    /// </summary>
    public void AddDummyRuns(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        _dummyRuns += count;
    }

    /// <summary>
    /// Merges two source tapes into this tape using provided buffers.
    /// </summary>
    public async Task<(string? consumedA, string? consumedB)> MergeFromAsync(
        Tape source1,
        Tape source2,
        string outputFilePath,
        MergeContext context,
        CancellationToken cancellationToken)
    {
        var pathA = source1.DequeuePathOnly();
        var pathB = source2.DequeuePathOnly();

        // Both dummy runs -> add dummy to output
        if (pathA == null && pathB == null)
        {
            AddDummyRuns(1);
            return (null, null);
        }

        // Merge files
        await MergeFilesAsync(pathA, pathB, outputFilePath, context, cancellationToken);
        Enqueue(outputFilePath);

        return (pathA, pathB);
    }

    private async Task MergeFilesAsync(
        string? pathA,
        string? pathB,
        string outputPath,
        MergeContext context,
        CancellationToken cancellationToken)
    {
        await using var writer = _fileSystem.FileWriter.CreateText(outputPath);
        
        using var readerA = pathA != null ? new BufferedFileReader(_fileSystem, pathA, context.BufferA) : null;
        using var readerB = pathB != null ? new BufferedFileReader(_fileSystem, pathB, context.BufferB) : null;

        var writeIndex = 0;

        // Initialize readers
        var hasA = readerA != null && await readerA.ReadNextAsync(cancellationToken);
        var hasB = readerB != null && await readerB.ReadNextAsync(cancellationToken);

        // Merge sorted sequences
        while (hasA && hasB)
        {
            var takeA = readerA!.CurrentKey.CompareTo(readerB!.CurrentKey) <= 0;
            var key = takeA ? readerA.CurrentKey : readerB.CurrentKey;

            if (!key.Value.Span.IsEmpty)
            {
                writeIndex = await writer
                    .WriteLineToBufferAsync(context.WriteBuffer, writeIndex, key.Value, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (takeA)
                hasA = await readerA.ReadNextAsync(cancellationToken);
            else
                hasB = await readerB.ReadNextAsync(cancellationToken);
        }

        // Drain remaining
        while (hasA)
        {
            if (!readerA!.CurrentKey.Value.Span.IsEmpty)
            {
                writeIndex = await writer
                    .WriteLineToBufferAsync(context.WriteBuffer, writeIndex, readerA.CurrentKey.Value, cancellationToken)
                    .ConfigureAwait(false);
            }
            hasA = await readerA.ReadNextAsync(cancellationToken);
        }

        while (hasB)
        {
            if (!readerB!.CurrentKey.Value.Span.IsEmpty)
            {
                writeIndex = await writer
                    .WriteLineToBufferAsync(context.WriteBuffer, writeIndex, readerB.CurrentKey.Value, cancellationToken)
                    .ConfigureAwait(false);
            }
            hasB = await readerB.ReadNextAsync(cancellationToken);
        }

        if (writeIndex > 0)
        {
            await writer.FlushBufferAsync(context.WriteBuffer, writeIndex, cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the number of items (files + dummy runs) on the tape.
    /// </summary>
    public int Count => _files.Count + _dummyRuns;

    /// <summary>
    /// Disposes the tape and clears the file queue.
    /// </summary>
    public void Dispose()
    {
        _files.Clear();
    }

    /// <summary>
    /// Reusable buffers for merge operations.
    /// </summary>
    public class MergeContext : IDisposable
    {
        internal readonly char[] BufferA;
        internal readonly char[] BufferB;
        internal readonly char[] WriteBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeContext"/> class.
        /// </summary>
        public MergeContext()
        {
            BufferA = ArrayPool<char>.Shared.Rent(BufferSize);
            BufferB = ArrayPool<char>.Shared.Rent(BufferSize);
            WriteBuffer = ArrayPool<char>.Shared.Rent(BufferSize);
        }

        /// <summary>
        /// Disposes the context and returns buffers to the pool.
        /// </summary>
        public void Dispose()
        {
            ArrayPool<char>.Shared.Return(BufferA);
            ArrayPool<char>.Shared.Return(BufferB);
            ArrayPool<char>.Shared.Return(WriteBuffer);
        }
    }

    /// <summary>
    /// Creates a new merge context with allocated buffers.
    /// </summary>
    /// <returns>A new <see cref="MergeContext"/>.</returns>
    public static MergeContext CreateMergeContext() => new();

    /// <summary>
    /// Encapsulates buffered reading logic for a single file.
    /// </summary>
    private sealed class BufferedFileReader : IDisposable
    {
        private readonly IStreamReader _reader;
        private readonly char[] _buffer;
        private int _pos;
        private int _len;

        public SortKey CurrentKey { get; private set; }

        public BufferedFileReader(IFileSystem fs, string path, char[] buffer)
        {
            _reader = fs.FileReader.OpenText(path);
            _buffer = buffer;
        }

        public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken)
        {
            // Fast path: try to read from buffer
            if (TryReadFromBuffer())
                return new ValueTask<bool>(true);

            // Slow path: need more data
            return ReadNextSlowAsync(cancellationToken);
        }

        private bool TryReadFromBuffer()
        {
            var span = _buffer.AsSpan(_pos, _len - _pos);
            var newlineIndex = span.IndexOf('\n');

            if (newlineIndex >= 0)
            {
                var lineLen = newlineIndex;
                if (lineLen > 0 && span[lineLen - 1] == '\r')
                    lineLen--;

                CurrentKey = new SortKey(new ReadOnlyMemory<char>(_buffer, _pos, lineLen), 0);

                _pos += newlineIndex + 1;
                return true;
            }

            return false;
        }

        private async ValueTask<bool> ReadNextSlowAsync(CancellationToken cancellationToken)
        {
            int bytesRead;
            
            do
            {
                var remaining = _len - _pos;
                if (remaining > 0 && _pos > 0)
                {
                    _buffer.AsSpan(_pos, remaining).CopyTo(_buffer);
                }
                _pos = 0;
                _len = remaining;

                // Read more data
                bytesRead = await _reader.ReadAsync(_buffer.AsMemory(_len), cancellationToken);
                _len += bytesRead;

                // Try to extract a line from the buffer
                if (TryReadFromBuffer())
                    return true;
                    
            } while (bytesRead > 0);
            
            // EOF reached - handle last line without newline
            if (_len > 0)
            {
                CurrentKey = new SortKey(new ReadOnlyMemory<char>(_buffer, 0, _len), 0);
                _pos = _len;
                return true;
            }
            
            return false;
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}