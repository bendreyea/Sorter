namespace App.ExternalSorter.Merge.Tournament;

using System.Runtime.CompilerServices;
using Configuration;
using FileSystem.Interfaces;

/// <summary>
/// Implements a tournament merge strategy for merging sorted files asynchronously using a tournament tree algorithm.
/// </summary>
public class TournamentMergeStrategyFiles : IMergeStrategy
{
    private IFileSystem _fileSystem;
    private static int _counter = 0;
    private readonly ExternalSorterSettings _settings;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TournamentMergeStrategyFiles"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system used for reading and writing files.</param>
    /// <param name="settings">The settings for the external sorter.</param>
    public TournamentMergeStrategyFiles(IFileSystem fileSystem, ExternalSorterSettings settings)
    {
        _fileSystem = fileSystem;
        _settings = settings;
    }
    
    /// <summary>
    /// Merges multiple sorted files into a single sorted output file using a tournament tree algorithm.
    /// </summary>
    /// <param name="sortedSequences">The collection of file paths representing sorted sequences to merge.</param>
    /// <param name="comparer">The comparer used to determine the order of elements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous merge operation, containing the path to the merged output file.</returns>
    public async Task<string> Merge(IEnumerable<string> sortedSequences, IComparer<string> comparer, CancellationToken cancellationToken)
    {
        var runs = sortedSequences.ToList();
        if (runs.Count == 0)
            throw new ArgumentException("At least one input file is required.", nameof(sortedSequences));

        // If we have few enough files, merge them directly
        if (runs.Count <= _settings.MergeBatch)
        {
            return await MergeBatch(runs, comparer, cancellationToken);
        }

        var createdFiles = new List<string>();
        string? finalFile = null;
        try
        {
            var chunks = runs.Chunk(_settings.MergeBatch);
            var tasks = new List<Task<string>>();
            var nextPassFiles = new List<string>();

            foreach (var chunk in chunks)
            {
                if (chunk.Length == 1)
                {
                    nextPassFiles.Add(chunk[0]);
                }
                else
                {
                    tasks.Add(MergeBatch(chunk, comparer, cancellationToken));
                }
            }

            var results = await Task.WhenAll(tasks);
            createdFiles.AddRange(results);
            nextPassFiles.AddRange(results);

            finalFile = await Merge(nextPassFiles, comparer, cancellationToken);
            return finalFile;
        }
        finally
        {
            if (createdFiles.Count > 0)
            {
                var toDelete = createdFiles.Where(f => f != finalFile);
                await Task.WhenAll(toDelete.Select(f => _fileSystem.DeleteFileAsync(f, cancellationToken)));
            }
        }
    }

    private async Task<string> MergeBatch(IList<string> runs, IComparer<string> comparer, CancellationToken cancellationToken)
    {
        if (runs.Count == 1)
            return runs[0];
        
        var listOfEnumerators = new List<IAsyncEnumerator<string>>(runs.Count);
        foreach (var run in runs)
        {
            listOfEnumerators.Add(GetAsyncEnumerableFromFile(run, cancellationToken).GetAsyncEnumerator(cancellationToken));
        }
        
        AsyncTournamentTree<string> tournamentTree = await AsyncTournamentTree<string>.CreateAsync(runs.Count, listOfEnumerators, comparer, cancellationToken);
        var merged =  tournamentTree.MergeAsync(cancellationToken);
        
        var outputPath = GenerateUniqueTempFileName();
        
        await using var writer = _fileSystem.FileWriter.CreateText(outputPath);
        await foreach (var item in merged.WithCancellation(cancellationToken))
        {
            await writer.WriteLineAsync(item);
        }

        await writer.FlushAsync(cancellationToken);

        return outputPath;
    }
    
    
    private async IAsyncEnumerable<string> GetAsyncEnumerableFromFile(string path, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var reader = _fileSystem.FileReader.OpenText(path);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            yield return line;
        }
    }
    
    /// <summary>
    /// Generates a unique temporary file name.
    /// </summary>
    private string GenerateUniqueTempFileName()
    {
        return Path.Combine(_settings.TempDirectory, $"temp_merge_{Interlocked.Increment(ref _counter)}{Constants.SortedFileExtension}");
    }
}