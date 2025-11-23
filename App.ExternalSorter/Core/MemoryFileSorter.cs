namespace App.ExternalSorter.Core;

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using Configuration;
using FileSystem.Interfaces;

/// <summary>
/// Pipeline-based file sorter.
/// </summary>
public class MemoryFileSorter
{
    private readonly IFileSystem _fileSystem;
    private readonly ExternalSorterSettings _settings;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryFileSorter"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="settings">The external sorter settings.</param>
    public MemoryFileSorter(IFileSystem fileSystem, ExternalSorterSettings settings)
    {
        _fileSystem = fileSystem;
        _settings = settings;
    }

    /// <summary>
    /// Sorts a file using high-performance pipeline processing.
    /// </summary>
    public async Task SortFileAsync(string unsortedFilePath, string targetPath, IComparer<string> comparer, CancellationToken token)
    {
        var (lines, count) = await ReadAllLinesWithPipelineAsync(unsortedFilePath, token);
        
        try
        {
            ThreeWayRadixQuickSort.Sort(lines, 0, count, comparer);
            await WriteLinesWithPipelineAsync(targetPath, lines, count, token);
        }
        finally
        {
            ArrayPool<string>.Shared.Return(lines);
        }
    }

    private async Task<(string[] Lines, int Count)> ReadAllLinesWithPipelineAsync(string filePath, CancellationToken token)
    {
        var reader = _fileSystem.FileReader.OpenAsPipeReader(filePath);
        var lines = ArrayPool<string>.Shared.Rent(_settings.InitialLineCapacity);
        var count = 0;
        var decoder = Encoding.UTF8.GetDecoder();
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(token);
                ReadOnlySequence<byte> buffer = result.Buffer;
                
                ProcessBuffer(ref buffer, ref lines, ref count, decoder, result.IsCompleted);
                
                reader.AdvanceTo(buffer.Start, buffer.End);
                
                if (result.IsCompleted)
                    break;
            }
        }
        catch
        {
            ArrayPool<string>.Shared.Return(lines);
            throw;
        }
        finally
        {
            await reader.CompleteAsync();
        }
        
        return (lines, count);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessBuffer(ref ReadOnlySequence<byte> buffer, ref string[] lines, ref int count,
        Decoder decoder, bool isCompleted)
    {
        Span<char> stackBuffer = stackalloc char[1024]; // Reusable stack buffer for small lines
        
        while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
        {
            if (count >= lines.Length)
            {
                var newSize = lines.Length * 2;
                var newArray = ArrayPool<string>.Shared.Rent(newSize);
                lines.AsSpan(0, count).CopyTo(newArray);
                ArrayPool<string>.Shared.Return(lines);
                lines = newArray;
            }

            // Use stackalloc for small lines, array pool for large ones
            if (line.Length <= 512)
            {
                int charCount = GetChars(line, stackBuffer, decoder);
                lines[count++] = new string(stackBuffer.Slice(0, charCount));
            }
            else
            {
                var charArray = ArrayPool<char>.Shared.Rent((int)line.Length * 2);
                try
                {
                    int charCount = GetChars(line, charArray, decoder);
                    lines[count++] = new string(charArray, 0, charCount);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(charArray);
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetChars(ReadOnlySequence<byte> bytes, Span<char> chars, Decoder decoder)
    {
        if (bytes.IsSingleSegment)
        {
            decoder.Convert(bytes.FirstSpan, chars, true, out _, out int charsWritten, out _);
            return charsWritten;
        }
        
        int totalChars = 0;
        foreach (var segment in bytes)
        {
            decoder.Convert(segment.Span, chars.Slice(totalChars), false, out _, out int charsWritten, out _);
            totalChars += charsWritten;
        }
        return totalChars;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var reader = new SequenceReader<byte>(buffer);
        
        if (reader.TryReadTo(out line, (byte)'\n'))
        {
            // Trim \r if present (Windows line ending)
            if (line.Length > 0 && line.Slice(line.Length - 1, 1).FirstSpan[0] == '\r')
            {
                line = line.Slice(0, line.Length - 1);
            }
            buffer = buffer.Slice(reader.Position);
            return true;
        }
        
        line = default;
        return false;
    }
    
    private async Task WriteLinesWithPipelineAsync(string targetPath, string[] lines, int count, CancellationToken token)
    {
        await using var writer = _fileSystem.FileWriter.CreateText(targetPath);
        var pipe = new Pipe(new PipeOptions(
            minimumSegmentSize: _settings.PipelineSegmentSize, 
            pauseWriterThreshold: _settings.PipelinePauseThreshold, 
            resumeWriterThreshold: _settings.PipelineResumeThreshold));
        
        // Start reader task
        var readerTask = ReadFromPipeAndWriteToStreamAsync(pipe.Reader, writer, token);
        
        // Write to pipe
        await WriteToPipeAsync(pipe.Writer, lines, count, token);
        
        // Wait for reader to complete
        await readerTask;
    }
    
    private async Task WriteToPipeAsync(PipeWriter writer, string[] lines, int count, CancellationToken token)
    {
        var newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
        
        try
        {
            for (int i = 0; i < count; i++)
            {
                var line = lines[i];
                token.ThrowIfCancellationRequested();
                
                // Get memory from pipe
                int maxBytes = Encoding.UTF8.GetMaxByteCount(line.Length) + newLineBytes.Length;
                Memory<byte> memory = writer.GetMemory(maxBytes);
                
                // Encode directly into pipe buffer
                int bytesWritten = Encoding.UTF8.GetBytes(
                    line.AsSpan(), memory.Span);
                
                // Add newline
                newLineBytes.CopyTo(memory.Span.Slice(bytesWritten));
                bytesWritten += newLineBytes.Length;
                
                // Advance writer
                writer.Advance(bytesWritten);
                
                // Flush periodically to avoid memory pressure
                if (writer.UnflushedBytes > _settings.FlushThreshold)
                {
                    FlushResult result = await writer.FlushAsync(token);
                    if (result.IsCompleted || result.IsCanceled)
                        break;
                }
            }
        }
        finally
        {
            await writer.CompleteAsync();
        }
    }
    
    private async Task ReadFromPipeAndWriteToStreamAsync(PipeReader reader, IStreamWriter writer, CancellationToken token)
    {
        try
        {
            var decoder = Encoding.UTF8.GetDecoder();
            var charBuffer = ArrayPool<char>.Shared.Rent(_settings.PipelineSegmentSize);
            
            try
            {
                while (!token.IsCancellationRequested)
                {
                    ReadResult result = await reader.ReadAsync(token);
                    ReadOnlySequence<byte> buffer = result.Buffer;
                    
                    // Decode and write to file
                    foreach (var segment in buffer)
                    {
                        int charCount = decoder.GetChars(segment.Span, charBuffer, false);
                        await writer.WriteAsync(charBuffer.AsMemory(0, charCount), token);
                    }
                    
                    reader.AdvanceTo(buffer.End);
                    
                    if (result.IsCompleted)
                        break;
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charBuffer);
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }
}