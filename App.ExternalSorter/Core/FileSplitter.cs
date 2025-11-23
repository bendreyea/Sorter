namespace App.ExternalSorter.Core;

using System.Buffers;
using System.Runtime.CompilerServices;
using Configuration;
using FileSystem.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Splits a large file into smaller chunks based on size.
/// </summary>
public class FileSplitter
{
    private readonly IFileSystem _fileSystem;
    private readonly ExternalSorterSettings _settings;
    private readonly ILogger<FileSplitter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSplitter"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="logger">The logger.</param>
    public FileSplitter(IFileSystem fileSystem, ExternalSorterSettings settings, ILogger<FileSplitter> logger)
    {
        _fileSystem = fileSystem;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Splits the file asynchronously and yields the filenames of the chunks.
    /// </summary>
    /// <param name="sourcePath">The source file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of filenames.</returns>
    public async IAsyncEnumerable<string> SplitFileAsync(string sourcePath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting file split: {SourcePath}, chunk size: {ChunkSize} bytes", sourcePath, _settings.BatchFileSize);
        var reader = _fileSystem.FileReader.OpenAsPipeReader(sourcePath);
        
        long currentFileIndex = 0;
        long currentFileSize = 0;
        var maxFileSize = _settings.BatchFileSize;
        
        IStreamWriter? currentWriter = null;
        string? currentFileName = null;
        bool isCompleted = false;
        var buffer = ReadOnlySequence<byte>.Empty;
        
        try
        {
            do
            {
                var result = await reader.ReadAsync(cancellationToken);
                buffer = result.Buffer;
                isCompleted = result.IsCompleted;
                
                // Skip empty buffers unless we're at the end
                if (buffer.IsEmpty && !isCompleted)
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                    continue;
                }
                
                var processResult = await ProcessBuffer(
                    buffer,
                    currentWriter,
                    currentFileName,
                    currentFileIndex,
                    currentFileSize,
                    maxFileSize,
                    cancellationToken);
                
                // Update state from result
                buffer = processResult.RemainingBuffer;
                currentWriter = processResult.CurrentWriter;
                currentFileName = processResult.CurrentFileName;
                currentFileIndex = processResult.CurrentFileIndex;
                currentFileSize = processResult.CurrentFileSize;
                
                // Yield completed files
                foreach (var file in processResult.CompletedFiles)
                {
                    _logger.LogDebug("Created chunk file: {ChunkFile}", file);
                    yield return file;
                }
                
                reader.AdvanceTo(processResult.ConsumedPosition, buffer.End);
                
            } while (!isCompleted);
            
            // Yield the last file if it exists
            if (currentWriter != null)
            {
                await currentWriter.DisposeAsync();
                _logger.LogDebug("Created final chunk file: {ChunkFile}", currentFileName);
                yield return currentFileName!;
            }
            
            _logger.LogInformation("File split complete. Created {ChunkCount} chunk files", currentFileIndex);
        }
        finally
        {
            currentWriter?.Dispose();
            await reader.CompleteAsync();
        }
    }
    
    private record ProcessBufferResult(
        ReadOnlySequence<byte> RemainingBuffer,
        SequencePosition ConsumedPosition,
        IStreamWriter? CurrentWriter,
        string? CurrentFileName,
        long CurrentFileIndex,
        long CurrentFileSize,
        List<string> CompletedFiles);
    
    private async Task<ProcessBufferResult> ProcessBuffer(
        ReadOnlySequence<byte> buffer,
        IStreamWriter? currentWriter,
        string? currentFileName,
        long currentFileIndex,
        long currentFileSize,
        long maxFileSize,
        CancellationToken cancellationToken)
    {
        var completedFiles = new List<string>();
        var consumed = buffer.Start;
        
        while (!buffer.IsEmpty)
        {
            // Create new file if needed
            if (currentWriter == null)
            {
                currentFileName = GenerateChunkFileName(++currentFileIndex);
                currentWriter = _fileSystem.FileWriter.CreateText(currentFileName);
                currentFileSize = 0;
            }
            
            // Check if we've reached the size limit
            if (currentFileSize >= maxFileSize)
            {
                // We've exceeded the limit - find next newline and split there
                var newlinePosition = buffer.PositionOf((byte)'\n');
                
                if (newlinePosition != null)
                {
                    // Write up to and including the newline
                    var splitPoint = buffer.GetPosition(1, newlinePosition.Value);
                    var slice = buffer.Slice(0, splitPoint);
                    
                    await currentWriter.WriteSequenceAsync(slice, cancellationToken);
                    await currentWriter.DisposeAsync();
                    completedFiles.Add(currentFileName!);
                    currentWriter = null;
                    currentFileName = null;
                    currentFileSize = 0;
                    
                    buffer = buffer.Slice(splitPoint);
                    consumed = splitPoint;
                }
                else
                {
                    await currentWriter.WriteSequenceAsync(buffer, cancellationToken);
                    currentFileSize += buffer.Length;
                    consumed = buffer.End;
                    buffer = buffer.Slice(buffer.End);
                }
            }
            else
            {
                // We haven't reached the limit yet
                long remainingSpace = maxFileSize - currentFileSize;
                
                if (buffer.Length <= remainingSpace)
                {
                    // Entire buffer fits - write it all
                    await currentWriter.WriteSequenceAsync(buffer, cancellationToken);
                    currentFileSize += buffer.Length;
                    consumed = buffer.End;
                    buffer = buffer.Slice(buffer.End);
                }
                else
                {
                    // Buffer exceeds remaining space - write only what fits
                    var slice = buffer.Slice(0, remainingSpace);
                    await currentWriter.WriteSequenceAsync(slice, cancellationToken);
                    currentFileSize += slice.Length;
                    buffer = buffer.Slice(slice.End);
                    consumed = slice.End;
                }
            }
        }
        
        return new ProcessBufferResult(
            buffer,
            consumed,
            currentWriter,
            currentFileName,
            currentFileIndex,
            currentFileSize,
            completedFiles);
    }
    
    private string GenerateChunkFileName(long index) => Path.Combine(_settings.TempDirectory, $"{index}.unsorted");
}