namespace App.ExternalSorter.IntegrationTests;

using System.Text;
using FluentAssertions;
using Generator;
using Configuration;
using Core;
using FileSystem.Implementations;
using FileSystem.Interfaces;
using Merge.PolyPhase;
using Microsoft.Extensions.Logging.Abstractions;
using Sorting;

public class FileSorterTests : IAsyncLifetime
{
    private const string UnsortedSource = "FileSorterTests.txt";
    private const string Sorted = "FileSorterTestsSorted.txt";
    private readonly IFileSystem _fileSystem;
    private readonly Sorter _sorter;
    private readonly FileGenerator _fileGenerator;
    private readonly IComparer<string> _comparer = new StringAndNumberComparer();
    private readonly string _dirPath;
    public FileSorterTests()
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

        var settings = new ExternalSorterSettings 
        { 
            TempDirectory = _dirPath,
            MergeBatch = 2,
            BatchFileSize =  512*1024, // 1 MB
        };
        var splitter = new FileSplitter(_fileSystem, settings, NullLogger<FileSplitter>.Instance);
        var merger = new PolyPhaseStrategyFiles(_fileSystem, settings, NullLogger<PolyPhaseStrategyFiles>.Instance);
        var logger = NullLogger<Sorter>.Instance;
        var sorter = new MemoryFileSorter(_fileSystem,settings);
        
        _sorter = new Sorter(splitter, sorter, logger, _fileSystem, settings, merger);
    }
    
    [Fact]
    public async Task SortFileAsync_ShouldSortFileInCorrectOrder()
    {
        var sortedInMemoryList = await ReadAllLines(Path.Combine(_dirPath, UnsortedSource));
        sortedInMemoryList.Sort(_comparer);
        await _sorter.Sort(Path.Combine(_dirPath, UnsortedSource), Path.Combine(_dirPath, Sorted), TestContext.Current.CancellationToken);
        var sortedList = await ReadAllLines(Path.Combine(_dirPath, Sorted));
        sortedList.Should().Equal(sortedInMemoryList);
    }
    
    public async ValueTask InitializeAsync()
    {
        await _fileGenerator.GenerateFile(Path.Combine(_dirPath, UnsortedSource), 20*1024*1024, TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _fileSystem.DeleteFile(Path.Combine(_dirPath, UnsortedSource));
        _fileSystem.DeleteFile(Path.Combine(_dirPath, Sorted));
        Directory.Delete(_dirPath);
        
        return ValueTask.CompletedTask;
    }
    
    private async Task<List<string>> ReadAllLines(string path)
    {
        using var streamReader = _fileSystem.FileReader.OpenText(path);
        var lines = new List<string>();
        while (!streamReader.EndOfStream)
        {
            lines.Add((await streamReader.ReadLineAsync())!);
        }

        return lines;
    }
}