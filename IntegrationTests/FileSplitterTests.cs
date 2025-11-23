namespace App.ExternalSorter.IntegrationTests;

using System.Text;
using FluentAssertions;
using App.FileSystem.Implementations;
using Configuration;
using Core;
using FileSystem.Interfaces;

public class FileSplitterTests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private readonly IFileSystem _fileSystem;
    private const string SourceFileName = "source.txt";

    public FileSplitterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "FileSplitterTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
        _fileSystem = new LocalFileSystem(new UTF8Encoding(false));
    }

    [Fact]
    public async Task SplitFileAsync_ShouldSplitFileIntoSmallerChunks()
    {
        // Arrange
        var sourceFilePath = Path.Combine(_testDirectory, SourceFileName);
        var content = new StringBuilder();

        for (int i = 0; i < 10; i++)
        {
            content.AppendLine($"Line {i} - This is some content to fill up the file.");
        }
        var contentString = content.ToString();
        
        await using (var writer = _fileSystem.FileWriter.CreateText(sourceFilePath))
        {
            await writer.WriteAsync(contentString.AsMemory(), TestContext.Current.CancellationToken);
        }

        var settings = new ExternalSorterSettings
        {
            TempDirectory = _testDirectory,
            BatchFileSize = 20 // Small batch size to force splitting
        };

        var splitter = new FileSplitter(_fileSystem, settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSplitter>.Instance);

        // Act
        var splitFiles = new List<string>();
        await foreach (var file in splitter.SplitFileAsync(sourceFilePath, TestContext.Current.CancellationToken))
        {
            splitFiles.Add(file);
        }

        // Assert
        splitFiles.Should().HaveCountGreaterThan(1);

        var reconstructedContent = new StringBuilder();
        foreach (var file in splitFiles)
        {
            await using var reader = _fileSystem.FileReader.OpenText(file);
            while (!reader.EndOfStream)
            {
                var str = await reader.ReadLineAsync(TestContext.Current.CancellationToken);
                if (str != null)
                    reconstructedContent.AppendLine(str);
            }
        }

        var actual = reconstructedContent.ToString();
        
        actual.Should().Be(contentString);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        await Task.CompletedTask;
    }
}
