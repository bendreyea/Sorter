namespace App.ExternalSorter.Benchmark;

using BenchmarkDotNet.Attributes;
using System.Text;
using Configuration;
using FileSystem.Implementations;
using FileSystem.Interfaces;
using Merge.MultiWay;
using Merge.PolyPhase;
using Merge.Tournament;
using Sorting;

/// <summary>
/// Benchmark class for testing file-based merge strategy performance.
/// </summary>
[MemoryDiagnoser]
public class FileMergeBenchmark
{
    private List<string> _inputFiles = new();
    private IFileSystem _fileSystem = null!;
    private ExternalSorterSettings _settings = null!;
    private string _tempDir = null!;
    private List<string> _filesToDelete = new();
    private readonly IComparer<string> _comparer = new StringAndNumberComparer();

    /// <summary>
    /// Sets up the benchmark data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FileMergeBenchmark_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        
        _fileSystem = new LocalFileSystem(Encoding.UTF8);
        _settings = new ExternalSorterSettings
        {
            TempDirectory = _tempDir
        };

        // Generate data
        var list = GenerateStringList(100000);
        var runs = MergeBenchmark.BuildRuns(list, 1000, _comparer); // 100 runs of 1000 items

        // Write runs to files
        int i = 0;
        var encoding = new UTF8Encoding(false); // Ensure no BOM
        foreach (var run in runs)
        {
            var file = Path.Combine(_tempDir, $"run_{i++}.sorted");
            using (var writer = new StreamWriter(file, false, encoding))
            {
                foreach (var item in run)
                {
                    writer.WriteLine(item);
                }
            }
            _inputFiles.Add(file);
        }
    }

    private List<string> GenerateStringList(int size)
    {
        Random rand = new Random(42);
        return Enumerable.Range(1, size)
                         .Select(i => $"{rand.Next(1, 200000)}. {RandomString(rand, rand.Next(5, 40))}")
                         .ToList();
    }

    private string RandomString(Random rand, int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        return new string(Enumerable.Repeat(chars, length)
                          .Select(s => s[rand.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Cleans up the benchmark data.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    /// <summary>
    /// Cleans up files created during iteration.
    /// </summary>
    [IterationCleanup]
    public void IterationCleanup()
    {
        foreach (var file in _filesToDelete)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        _filesToDelete.Clear();
    }

    /// <summary>
    /// Benchmarks the K-way file-based merge strategy.
    /// </summary>
    [Benchmark]
    public async Task BenchmarkKWayFiles()
    {
        var strategy = new KWayStrategyFiles(_fileSystem, _settings);
        var result = await strategy.Merge(_inputFiles, _comparer, CancellationToken.None);
        _filesToDelete.Add(result);
    }

    /// <summary>
    /// Benchmarks the Polyphase file-based merge strategy.
    /// </summary>
    [Benchmark]
    public async Task BenchmarkPolyPhaseFiles()
    {
        var strategy = new PolyPhaseStrategyFiles(_fileSystem, _settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<PolyPhaseStrategyFiles>.Instance);
        var result = await strategy.Merge(_inputFiles, _comparer, CancellationToken.None);
        _filesToDelete.Add(result);
    }

    /// <summary>
    /// Benchmarks the Tournament file-based merge strategy.
    /// </summary>
    [Benchmark]
    public async Task BenchmarkTournamentFiles()
    {
        var strategy = new TournamentMergeStrategyFiles(_fileSystem, _settings);
        var result = await strategy.Merge(_inputFiles, _comparer, CancellationToken.None);
        _filesToDelete.Add(result);
    }
}
