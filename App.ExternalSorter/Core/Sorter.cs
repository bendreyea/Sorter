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
             SingleReader = true
        });
        
        string? finalSortedFile = null;
        
        // Start all tasks concurrently
        var splitterTask = SplitAndWriteToChannelAsync(inputFilePath, channelPartitioner.Writer, cancellationToken);
        var producerTask = Producer(channelPartitioner.Reader, channelMerge.Writer,"Producer1", cancellationToken);
        var consumerTask = Consumer(channelMerge.Reader, "Consumer", cancellationToken, mergedFile => finalSortedFile = mergedFile);
        var producerTask2 = Producer(channelPartitioner.Reader, channelMerge.Writer,"Producer2", cancellationToken);

        // Wait for splitter and producer to finish
        await Task.WhenAll(splitterTask, producerTask, producerTask2).ConfigureAwait(false);
        channelMerge.Writer.Complete();

         // Wait for consumer to finish processing
         await consumerTask.ConfigureAwait(false);
         
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

        // Iterate over the collection
        await foreach (var item in reader.ReadAllAsync(token).ConfigureAwait(false))
        {
            token.ThrowIfCancellationRequested();
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _logger?.LogInformation("Sorting chunk file: {Filename}", item);
            var sortedFilename = item.Replace(Constants.UnsortedFileExtension, Constants.SortedFileExtension);
            var unsortedFilePath = GetFullPath(item);
            var sortedFilePath = GetFullPath(sortedFilename);
            await _memoryFileSorter.SortFileAsync(unsortedFilePath, sortedFilePath, _comparer, token).ConfigureAwait(false);
            sw.Stop();
            _logger?.LogInformation("Sorted chunk file: {UnsortedFile} -> {SortedFile} in {ElapsedMs}ms", Path.GetFileName(unsortedFilePath), Path.GetFileName(sortedFilePath), sw.ElapsedMilliseconds);
            
            // don't have async version
            deleteFiles.Add(_fileSystem.DeleteFileAsync(unsortedFilePath, token));
            await writer.WriteAsync(sortedFilename, token);            
        }
    
        await Task.WhenAll(deleteFiles).ConfigureAwait(false);
     }

    private async Task Consumer(ChannelReader<string> reader, string consumerName, CancellationToken token, Action<string>? onComplete = null)
    {
        var batch = new List<string>();
        var mergeCount = 0;
        
        await foreach (var item in reader.ReadAllAsync(token).ConfigureAwait(false))
        {
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
            // Invoke the callback with the final file path
            onComplete?.Invoke(batch[0]);
        }
        else if (batch.Count == 0)
        {
            _logger?.LogWarning("{ConsumerName}: No files to merge", consumerName);
        }
    }
        
    private string GetFullPath(string filename)
    {
        var path = string.Intern(Path.Combine(_settings.TempDirectory, Path.GetFileName(filename)));
        return path;
    }
}