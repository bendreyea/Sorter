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
    private const int MinBufferSize = 65536; // 64KB minimum buffer
    
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
        // Step 1: Read lines efficiently using pipelines
        var allLines = await ReadAllLinesWithPipelineAsync(unsortedFilePath, token);
        
        // Step 2: Sort
        allLines.Sort(comparer);
        
        // Step 3: Write using pipeline writer
        await WriteLinesWithPipelineAsync(targetPath, allLines, token);
    }

    private async Task<List<string>> ReadAllLinesWithPipelineAsync(string filePath, CancellationToken token)
    {
        var reader = _fileSystem.FileReader.OpenAsPipeReader(filePath);
        var allLines = new List<string>();
        var decoder = Encoding.UTF8.GetDecoder();
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(token);
                ReadOnlySequence<byte> buffer = result.Buffer;
                
                ProcessBuffer(ref buffer, allLines, decoder, result.IsCompleted);
                
                reader.AdvanceTo(buffer.Start, buffer.End);
                
                if (result.IsCompleted)
                    break;
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }
        
        return allLines;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessBuffer(ref ReadOnlySequence<byte> buffer, List<string> lines, 
        Decoder decoder, bool isCompleted)
    {
        Span<char> stackBuffer = stackalloc char[1024]; // Reusable stack buffer for small lines
        
        while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
        {
            // Use stackalloc for small lines, array pool for large ones
            if (line.Length <= 512)
            {
                int charCount = GetChars(line, stackBuffer, decoder);
                lines.Add(new string(stackBuffer.Slice(0, charCount)));
            }
            else
            {
                var charArray = ArrayPool<char>.Shared.Rent((int)line.Length * 2);
                try
                {
                    int charCount = GetChars(line, charArray, decoder);
                    lines.Add(new string(charArray, 0, charCount));
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
    
    private async Task WriteLinesWithPipelineAsync(string targetPath, IEnumerable<string> lines, CancellationToken token)
    {
        await using var writer = _fileSystem.FileWriter.CreateText(targetPath);
        var pipe = new Pipe(new PipeOptions(minimumSegmentSize: MinBufferSize, pauseWriterThreshold: MinBufferSize * 4, resumeWriterThreshold: MinBufferSize * 2));
        
        // Start reader task
        var readerTask = ReadFromPipeAndWriteToStreamAsync(pipe.Reader, writer, token);
        
        // Write to pipe
        await WriteToPipeAsync(pipe.Writer, lines, token);
        
        // Wait for reader to complete
        await readerTask;
    }
    
    private async Task WriteToPipeAsync(PipeWriter writer, IEnumerable<string> lines, CancellationToken token)
    {
        var newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
        
        try
        {
            foreach (var line in lines)
            {
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
                if (writer.UnflushedBytes > MinBufferSize)
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
            var charBuffer = ArrayPool<char>.Shared.Rent(MinBufferSize);
            
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