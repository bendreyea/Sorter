namespace App.ExternalSorter.Benchmark;

using BenchmarkDotNet.Attributes;
using App.Generator;
using System.Text;
using Configuration;
using Core;
using FileSystem.Implementations;
using FileSystem.Interfaces;
using Merge.PolyPhase;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Benchmark class for testing Sorter performance with a 1GB file.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 0, iterationCount: 3)]
public class SorterBenchmark
{
    private string _inputFile = null!;
    private string _outputFile = null!;
    private string _tempDir = null!;
    private IFileSystem _fileSystem = null!;
    private ExternalSorterSettings _settings = null!;
    private FileSplitter _splitter = null!;
    private Sorter _sorter = null!;

    /// <summary>
    /// Sets up the benchmark data - generates a 1GB file.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SorterBenchmark_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        
        _inputFile = Path.Combine(_tempDir, "input_1gb.txt");
        _outputFile = Path.Combine(_tempDir, "output_1gb.txt");
        
        _fileSystem = new LocalFileSystem(Encoding.UTF8);
        _settings = new ExternalSorterSettings
        {
            TempDirectory = _tempDir,
            BatchFileSize = 16 * 1024 * 1024, // 10 MB chunks
            MergeBatch = 64
        };

        // Generate 1GB file
        GenerateFile(_inputFile, 1L * 1024 * 1024 * 1024); // 1GB

        // Initialize components
        _splitter = new FileSplitter(_fileSystem, _settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSplitter>.Instance);
        var logger = NullLogger<Sorter>.Instance;
        var merger = new PolyPhaseStrategyFiles(_fileSystem, _settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<PolyPhaseStrategyFiles>.Instance);
        var sorter = new MemoryFileSorter(_fileSystem, _settings);
        _sorter = new Sorter(_splitter, sorter, logger, _fileSystem, _settings, merger);
    }

    private void GenerateFile(string filePath, long targetSizeBytes)
    {
        var fileGenerator = new FileGenerator(_fileSystem);
        fileGenerator.GenerateFile(filePath, targetSizeBytes, CancellationToken.None).GetAwaiter().GetResult();
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
        if (File.Exists(_outputFile))
        {
            File.Delete(_outputFile);
        }
        
        // Clean up any leftover temp files
        var tempFiles = Directory.GetFiles(_tempDir, "*.sorted")
            .Concat(Directory.GetFiles(_tempDir, "*.unsorted"))
            .Concat(Directory.GetFiles(_tempDir, "*.merged"));
        
        foreach (var file in tempFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Benchmarks the Sorter.Sort method with a 1GB file.
    /// </summary>
    [Benchmark]
    public async Task Sort1GBFile()
    {
        await _sorter.Sort(_inputFile, _outputFile, CancellationToken.None);
    }
}
