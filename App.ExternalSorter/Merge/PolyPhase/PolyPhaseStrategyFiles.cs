namespace App.ExternalSorter.Merge.PolyPhase;

using App.FileSystem;
using Configuration;
using FileSystem.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements polyphase merge sort strategy using files.
/// </summary>
public class PolyPhaseStrategyFiles : IMergeStrategy
{
    private IFileSystem _fileSystem;
    private static int _counter = 0;
    private readonly ExternalSorterSettings _settings;
    private ILogger<PolyPhaseStrategyFiles> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolyPhaseStrategyFiles"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="settings">The external sorter settings.</param>
    /// <param name="logger">The logger.</param>
    public PolyPhaseStrategyFiles(IFileSystem fileSystem, ExternalSorterSettings settings, ILogger<PolyPhaseStrategyFiles> logger)
    {
        _fileSystem = fileSystem;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Merges multiple sorted files into a single sorted file using polyphase merge.
    /// </summary>
    /// <param name="sortedSequences">The paths to the sorted files.</param>
    /// <param name="comparer">The comparer used to determine the order of elements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the merged sorted file.</returns>
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
        var runCount = runs.Count;
        var (a, b, dummy) = runCount.FibonacciSplit();

        using var tape1 = new Tape(a, _fileSystem);
        using var tape2 = new Tape(b + dummy, _fileSystem);
        using var tape3 = new Tape(0, _fileSystem);

        int idx = 0;
        for (; idx < a; idx++)
            tape1.Enqueue(runs[idx]);
        for (; idx < a + b; idx++)
            tape2.Enqueue(runs[idx]);

        tape2.AddDummyRuns(dummy);

        Tape[] tapes = [tape1, tape2, tape3];
        var tempFiles = new HashSet<string>();
        await RunMergePhases(tapes, comparer, tempFiles, cancellationToken);

        // Copy final tape back to caller's array
        var final = tapes[FindNonEmptyTape(tapes)].GetCurrentFilePath();
        return final ?? throw new InvalidOperationException("No output file was produced from merge operation.");
    }

    private async Task RunMergePhases(Tape[] tapes, IComparer<string> comparer, HashSet<string> tempFiles, CancellationToken cancellationToken)
    {
        int output = FindEmptyTape(tapes); // start with the dummy/empty tape

        using var context = Tape.CreateMergeContext();

        while (TotalRuns(tapes) > 1)
        {
            int in1 = (output + 1) % 3;
            int in2 = (output + 2) % 3;

            // if one of them happens to be empty, swap so that in1 has runs
            if (tapes[in1].Count == 0 && tapes[in2].Count > 0)
                (in1, in2) = (in2, in1);

            int mergePairs = Math.Min(tapes[in1].Count, tapes[in2].Count);
            if (mergePairs == 0) // safety: nothing left to merge
                break;

            for (int i = 0; i < mergePairs; i++)
            {
                var outputFile = GenerateUniqueTempFileName();

                var (consumedA, consumedB) = await tapes[output].MergeFromAsync(
                    tapes[in1], 
                    tapes[in2], 
                    outputFile, 
                    context, 
                    cancellationToken);

                if (consumedA != null || consumedB != null)
                {
                    tempFiles.Add(outputFile);
                }

                if (consumedA != null && tempFiles.Remove(consumedA))
                {
                    await _fileSystem.DeleteFileAsync(consumedA, cancellationToken);
                }

                if (consumedB != null && tempFiles.Remove(consumedB))
                {
                    await _fileSystem.DeleteFileAsync(consumedB, cancellationToken);
                }
            }

            // the input tape that just became empty takes over as output
            output = tapes[in1].Count == 0 ? in1 : in2;
        }
    }
    
    /// <summary>
    /// Generates a unique temporary file name.
    /// </summary>
    private string GenerateUniqueTempFileName()
    {
        return Path.Combine(_settings.TempDirectory, $"temp_merge_{Interlocked.Increment(ref _counter)}{Constants.SortedFileExtension}");
    }

    private static int FindEmptyTape(Tape[] t) => t[0].Count == 0 ? 0 : t[1].Count == 0 ? 1 : 2;

    private static int FindNonEmptyTape(Tape[] t) => t[0].Count != 0 ? 0 : t[1].Count != 0 ? 1 : 2;

    private static int TotalRuns(Tape[] t) => t[0].Count + t[1].Count + t[2].Count;
}