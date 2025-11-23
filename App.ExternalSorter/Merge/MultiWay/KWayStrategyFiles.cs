namespace App.ExternalSorter.Merge.MultiWay;

using Configuration;
using FileSystem.Interfaces;

/// <summary>
/// Implements a k-way merge strategy using files.
/// </summary>
public class KWayStrategyFiles : IMergeStrategy
{
    private readonly IFileSystem _fileSystem;
    private readonly ExternalSorterSettings _settings;
    private static int _counter = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="KWayStrategyFiles"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="settings">The external sorter settings.</param>
    public KWayStrategyFiles(IFileSystem fileSystem, ExternalSorterSettings settings)
    {
        _fileSystem = fileSystem;
        _settings = settings;
    }

    /// <summary>
    /// Merges multiple sorted files into a single sorted file.
    /// </summary>
    /// <param name="sortedSequences">The paths to the sorted files.</param>
    /// <param name="comparer">The comparer used to determine the order of elements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the merged sorted file.</returns>
    public async Task<string> Merge(IEnumerable<string> sortedSequences, IComparer<string> comparer, CancellationToken cancellationToken)
    {
        var runs = sortedSequences.ToList();
        if (runs.Count == 0)
        {
            var emptyFile = GenerateUniqueTempFileName();
            await using var writer = _fileSystem.FileWriter.CreateText(emptyFile);
            return emptyFile;
        }

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

    private async Task<string> MergeBatch(IList<string> files, IComparer<string> comparer, CancellationToken cancellationToken)
    {
        var readers = new IStreamReader[files.Count];
        var pq = new PriorityQueue<(int index, string value), string>(comparer);

        try 
        {
            // Open all files
            for (int i = 0; i < files.Count; i++)
            {
                readers[i] = _fileSystem.FileReader.OpenText(files[i]);
                var line = await readers[i].ReadLineAsync(cancellationToken);
                if (line != null)
                {
                    pq.Enqueue((i, line), line);
                }
            }

            var outputFile = GenerateUniqueTempFileName();
            await using var writer = _fileSystem.FileWriter.CreateText(outputFile);

            while (pq.TryDequeue(out var node, out _))
            {
                if (!string.IsNullOrWhiteSpace(node.value))
                {
                    await writer.WriteLineAsync(node.value);
                }

                var nextLine = await readers[node.index].ReadLineAsync(cancellationToken);
                if (nextLine != null)
                {
                    pq.Enqueue((node.index, nextLine), nextLine);
                }
            }
            
            await writer.FlushAsync(cancellationToken);
            return outputFile;
        }
        finally
        {
            foreach (var reader in readers)
            {
                if (reader != null)
                {
                    await reader.DisposeAsync();
                }
            }
        }
    }

    private string GenerateUniqueTempFileName()
    {
        return Path.Combine(_settings.TempDirectory, $"kway_merge_{Interlocked.Increment(ref _counter)}{Constants.SortedFileExtension}");
    }
}
