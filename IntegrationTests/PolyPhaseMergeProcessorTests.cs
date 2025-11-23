namespace App.ExternalSorter.IntegrationTests;

using System.Text;
using FluentAssertions;
using App.Generator;
using Configuration;
using FileSystem.Implementations;
using FileSystem.Interfaces;
using Merge;
using Merge.PolyPhase;
using Sorting;
using Xunit;

public class PolyPhaseMergeProcessorTests : IAsyncLifetime
{
    private readonly FileGenerator _fileGenerator;
    private readonly IFileSystem _fileSystem;
    private readonly IComparer<string> _comparer = new StringAndNumberComparer();
    private readonly IMergeStrategy _mergeProcessor;
    private readonly string _merged = "Merged.txt";
    private readonly string _dirPath;
    
    private readonly List<string> _collection = new List<string>();
    
    public PolyPhaseMergeProcessorTests()
    {
        Encoding encoding = new UTF8Encoding(false);
        _fileSystem = new LocalFileSystem(encoding);
        _fileGenerator = new FileGenerator(_fileSystem);
        
        var uuid = Guid.NewGuid().ToString();
        _dirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, uuid);
        if (!Directory.Exists(_dirPath))
        {
            Directory.CreateDirectory(_dirPath);
        }
        
        var settings = new ExternalSorterSettings()
        {
            TempDirectory = _dirPath,
            MergeBatch = 2 // Small batch to test recursion
        };
        
        _mergeProcessor = new PolyPhaseStrategyFiles(_fileSystem, settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<PolyPhaseStrategyFiles>.Instance);
    }
    
    [Fact]
    public async Task MergeFilesAsync_ShouldSortFileInCorrectOrder()
    {
        var allLines = new List<string>();
        foreach(var file in _collection)
        {
            allLines.AddRange(await ReadAllLines(file));
        }
        allLines.Sort(_comparer);
        
        var resultPath = await _mergeProcessor.Merge(_collection, _comparer, TestContext.Current.CancellationToken);
        
        var mergedList = await ReadAllLines(resultPath);
        
        mergedList.Should().Equal(allLines);
        
        // Cleanup
        _fileSystem.DeleteFile(resultPath);
    }
    
    public async ValueTask InitializeAsync()
    {
        for (var i = 0; i < 100; i++)
        {
            var unsortedFile = Path.Combine(_dirPath, $"file{i}{Constants.UnsortedFileExtension}");
            await _fileGenerator.GenerateFile(unsortedFile, 1024*100, TestContext.Current.CancellationToken);
            var list = await ReadAllLines(unsortedFile);
            list.Sort(_comparer);
            var filename = Path.Combine(_dirPath, $"file{i}{Constants.SortedFileExtension}");
            await WriteAllLines(filename, list);
            _collection.Add(filename);
            _fileSystem.DeleteFile(unsortedFile);
        }
    }
    

    public ValueTask DisposeAsync()
    {
        foreach (var name in _collection)
        {
            _fileSystem.DeleteFile(name);
        }
        
        _fileSystem.DeleteFile(Path.Combine(_dirPath, _merged));
        
        if (Directory.Exists(_dirPath))
            Directory.Delete(_dirPath, true);
        
        return ValueTask.CompletedTask;
    }
    
    private async Task<List<string>> ReadAllLines(string path)
    {
        await using var streamReader = _fileSystem.FileReader.OpenText(path);
        var lines = new List<string>();
        while (!streamReader.EndOfStream)
        {
            var line = await streamReader.ReadLineAsync();
            if (line != null)
                lines.Add(line);
        }

        return lines;
    }

    private async Task WriteAllLines(string path, List<string> lines)
    {
        await using var fileWriter = _fileSystem.FileWriter.CreateText(path);
        foreach (var line in lines)
        {
            await fileWriter.WriteLineAsync(line);
        }

        await fileWriter.FlushAsync();
    }
}
