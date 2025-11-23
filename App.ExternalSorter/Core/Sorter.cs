namespace App.ExternalSorter.Core;

using System.Threading.Channels;
using Configuration;
using FileSystem.Interfaces;
using Merge;
using Microsoft.Extensions.Logging;
using Sorting;

/// <summary>
/// Orchestrates the external sorting process by splitting, sorting chunks, and merging them.
/// </summary>
public class Sorter
{
    private readonly IComparer<string> _comparer = new StringAndNumberComparer();
    private readonly ExternalSorterSettings _settings;
    private readonly IMergeStrategy _merger;
    private readonly IFileSystem _fileSystem;
    private readonly FileSplitter _splitter;
    private readonly MemoryFileSorter _memoryFileSorter;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sorter"/> class.
    /// </summary>
    /// <param name="splitter">The file splitter.</param>
    /// <param name="memoryFileSorter">The in-memory file sorter.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="merger">The merge strategy.</param>
    public Sorter(FileSplitter splitter, MemoryFileSorter memoryFileSorter, ILogger<Sorter> logger, IFileSystem fileSystem, ExternalSorterSettings settings, IMergeStrategy merger)
    {
        _splitter = splitter;
        _memoryFileSorter = memoryFileSorter;
        _logger = logger;
        _fileSystem = fileSystem;
        _settings = settings;
        _merger = merger;
    }
    
    /// <summary>
    /// Sorts the input file and writes the result to the output file.
    /// </summary>
    /// <param name="inputFilePath">The path to the input file.</param>
    /// <param name="outputFilePath">The path to the output file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Sort(string inputFilePath, string outputFilePath, CancellationToken cancellationToken)
    {
        var channelPartitioner = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
             SingleWriter = true,
             // Multiple consumers can read concurrently
             SingleReader = false
        });
        
        var channelMerge = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
             SingleWriter = false,
             // Multiple consumers can read concurrently
             SingleReader = false
        });
        
        // Start all tasks concurrently
        var splitterTask = SplitAndWriteToChannelAsync(inputFilePath, channelPartitioner.Writer, cancellationToken);
        
        var producers = new List<Task>();
        int producerCount = _settings.MaxConcurrency;

        for (int i = 0; i < producerCount; i++)
        {
            producers.Add(Producer(channelPartitioner.Reader, channelMerge.Writer, $"Producer{i+1}", cancellationToken));
        }

        var consumers = new List<Task<string?>>();
        int consumerCount = _settings.MaxConcurrency;
        for (int i = 0; i < consumerCount; i++)
        {
            consumers.Add(Consumer(channelMerge.Reader, $"Consumer{i+1}", cancellationToken));
        }

        // Wait for splitter and producer to finish
        await splitterTask.ConfigureAwait(false);
        await Task.WhenAll(producers).ConfigureAwait(false);
        channelMerge.Writer.Complete();

         // Wait for consumer to finish processing
         var consumerResults = await Task.WhenAll(consumers).ConfigureAwait(false);
         
         var finalFiles = consumerResults.Where(f => f != null).Cast<string>().ToList();
         
         string? finalSortedFile = null;
         
         if (finalFiles.Count == 0)
         {
             throw new InvalidOperationException("No sorted file was produced");
         }
         else if (finalFiles.Count == 1)
         {
             finalSortedFile = finalFiles[0];
         }
         else
         {
             // Final merge of the results from multiple consumers
             _logger?.LogInformation("Performing final merge of {Count} files from consumers", finalFiles.Count);
             finalSortedFile = await _merger.Merge(finalFiles, _comparer, cancellationToken).ConfigureAwait(false);
             
             // Cleanup intermediate files
             foreach(var file in finalFiles)
             {
                 await _fileSystem.DeleteFileAsync(file, cancellationToken);
             }
         }
         
         // Move the final sorted file to the output location
         if (!string.IsNullOrEmpty(finalSortedFile))
         {
            _fileSystem.MoveFile(finalSortedFile, outputFilePath, overwrite: true);
             _logger?.LogInformation("Sorting complete. Output file: {OutputFile}", outputFilePath);
         }
         else
         {
             throw new InvalidOperationException("No sorted file was produced");
         }
    }
    
    private async Task SplitAndWriteToChannelAsync(string inputFilePath, ChannelWriter<string> writer, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var chunk in _splitter.SplitFileAsync(inputFilePath, cancellationToken))
            {
                await writer.WriteAsync(chunk, cancellationToken);
            }
        }
        finally
        {
            // Signal that no more items will be written
            writer.Complete();
        }
    }

    private async Task Producer(ChannelReader<string> reader, ChannelWriter<string> writer, string producerName, CancellationToken token)
    {
        var deleteFiles = new List<Task>();
        
        try
        {
            await foreach (var item in reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                _logger?.LogInformation("{Producer}: Sorting chunk file: {Filename}", producerName, item);
                
                var sortedFilename = item.Replace(Constants.UnsortedFileExtension, Constants.SortedFileExtension);
                var unsortedFilePath = GetFullPath(item);
                var sortedFilePath = GetFullPath(sortedFilename);
                
                await _memoryFileSorter.SortFileAsync(unsortedFilePath, sortedFilePath, _comparer, token).ConfigureAwait(false);
                
                sw.Stop();
                _logger?.LogInformation("{Producer}: Sorted in {ElapsedMs}ms", producerName, sw.ElapsedMilliseconds);
                
                deleteFiles.Add(_fileSystem.DeleteFileAsync(unsortedFilePath, token));
                await writer.WriteAsync(sortedFilename, token);
            }
            
            await Task.WhenAll(deleteFiles).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ensure cleanup happens even on cancellation
            await Task.WhenAll(deleteFiles).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<string?> Consumer(ChannelReader<string> reader, string consumerName, CancellationToken token)
    {
        var batch = new List<string>();
        var mergeCount = 0;
        
        await foreach (var item in reader.ReadAllAsync(token).ConfigureAwait(false))
        {
            _logger?.LogInformation("{ConsumerName}: Receiving {item}", consumerName, item);

            token.ThrowIfCancellationRequested();
            batch.Add(GetFullPath(item));
            
            // When batch reaches the configured size, merge the files
            if (batch.Count >= _settings.MergeBatch)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                _logger?.LogInformation("{ConsumerName}: Merging batch {MergeCount} with {BatchCount} files", consumerName, mergeCount++, batch.Count);
                    
                var mergedFile = await _merger.Merge(batch, _comparer, token).ConfigureAwait(false);
                sw.Stop();
                _logger?.LogInformation("{ConsumerName}: Merged batch into: {MergedFile} in {ElapsedMs}ms", consumerName, Path.GetFileName(mergedFile), sw.ElapsedMilliseconds);
                
                // Delete the source files that were merged (but not the merged file itself)
                var filesToDelete = batch.ToList(); // Create a copy
                var deleteTasks = filesToDelete.Select(file => _fileSystem.DeleteFileAsync(file, token));
                await Task.WhenAll(deleteTasks).ConfigureAwait(false);
                
                // Clear the batch and start new one with the merged file
                batch.Clear();
                batch.Add(mergedFile);
            }
        }
        
        // Handle any remaining files after channel completion
        while (batch.Count > 1)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _logger?.LogInformation("{ConsumerName}: Final merge of {BatchCount} files", consumerName, batch.Count);
            
            // Take up to MergeBatch files or all remaining files
            var batchSize = Math.Min(_settings.MergeBatch, batch.Count);
            var filesToMerge = batch.Take(batchSize).ToList();
            var mergedFile = await _merger.Merge(filesToMerge, _comparer, token).ConfigureAwait(false);
            sw.Stop();
            _logger?.LogInformation("{ConsumerName}: Final merged into: {MergedFile} in {ElapsedMs}ms", consumerName, Path.GetFileName(mergedFile), sw.ElapsedMilliseconds);
            
            // Delete the merged source files
            var deleteTasks = filesToMerge.Select(file => _fileSystem.DeleteFileAsync(file, token));
            await Task.WhenAll(deleteTasks).ConfigureAwait(false);
            
            // Remove merged files from batch and add the result
            batch.RemoveAll(f => filesToMerge.Contains(f));
            batch.Add(mergedFile);
        }
        
        // The final file should be moved to the output location
        if (batch.Count == 1)
        {
            _logger?.LogInformation("{ConsumerName}: External sort complete. Final file: {FinalFile}", consumerName, batch[0]);
            return batch[0];
        }
        else if (batch.Count == 0)
        {
            _logger?.LogWarning("{ConsumerName}: No files to merge", consumerName);
            return null;
        }
        
        return null;
    }
        
    private string GetFullPath(string filename)
    {
        var path = string.Intern(Path.Combine(_settings.TempDirectory, Path.GetFileName(filename)));
        return path;
    }
}