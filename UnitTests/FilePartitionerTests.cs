namespace App.ExternalSorter.UnitTests;

using System.IO.Pipelines;
using System.Text;
using Configuration;
using Core;
using FluentAssertions;
using NSubstitute;
using App.FileSystem.InMemory;
using FileSystem.Interfaces;
using Xunit;

public class FilePartitionerTests
{
    [Fact]
    public async Task SplitFile_SplitsFileCorrectly_WithMultiCharNewline()
    {
        // Arrange
        var tempDir = "TestFiles";
        var options = new ExternalSorterSettings()
        {
            TempDirectory     = tempDir,
            BatchFileSize     = 8,
        };
        
        // Prepare in-memory source reader
        var sourceContent = "Line1\r\nLine2\r\nLine3\r\n";
        var unsortedFileNames = new List<string>();
        var unsortedWriters = new List<InMemoryStreamWriter>();
        var fileSystem = CreateTestFileSystem(sourceContent, unsortedFileNames, unsortedWriters, "\r\n");

        // Act
        var filePartitioner = new FileSplitter(fileSystem, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSplitter>.Instance);
        var filenames = filePartitioner.SplitFileAsync("sourcePath",  TestContext.Current.CancellationToken);
        var names =  await filenames.ToArrayAsync(TestContext.Current.CancellationToken);
        
        // Assert
        // FileSplitter writes data until size limit (8 bytes) is reached, then continues to next newline
        // "Line1\r\n" = 7 bytes (under limit) -> writes it
        // "Line2\r\n" = 7 more bytes (total 14, over limit of 8) -> continues to newline and splits
        // First chunk: "Line1\r\nLine2\r\n" (14 bytes)
        // Second chunk: "Line3\r\n" (7 bytes)
        names.Should().HaveCount(2);
        unsortedWriters[0].Content.Should().Be("Line1\r\nLine2\r\n");
        unsortedWriters[1].Content.Should().Be("Line3\r\n");
    }
    
    [Fact]
    public async Task SplitFile_SplitsFileCorrectly_WithOneBigString()
    {
        // Arrange
        var tempDir = "TestFiles";
        var options = new ExternalSorterSettings()
        {
            TempDirectory     = tempDir,
            BatchFileSize     = 50,
        };
        
        
        // Prepare in-memory source reader
        var sourceContent = "Line1Line2Line3\n";
        var unsortedFileNames = new List<string>();
        var unsortedWriters = new List<InMemoryStreamWriter>();
        var fileSystem = CreateTestFileSystem(sourceContent, unsortedFileNames, unsortedWriters, "\n");

        // Act
        var filePartitioner = new FileSplitter(fileSystem, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSplitter>.Instance);
        var filenames = filePartitioner.SplitFileAsync("sourcePath",  TestContext.Current.CancellationToken);
        var names =  await filenames.ToArrayAsync(TestContext.Current.CancellationToken);
        
        // Assert
        names.Should().HaveCount(1);
        // Verify the content written to each writer
        unsortedWriters[0].Content.Should().Be("Line1Line2Line3\n");
    }
    
    [Fact]
    public async Task SplitFile_SplitsFileCorrectly_WithSingleCharNewline()
    {
        // Arrange
        var tempDir = "TestFiles";
        var options = new ExternalSorterSettings()
        {
            TempDirectory = tempDir,
            BatchFileSize = 4,
        };

        var sourceContent = "Line1\nLine2\nLine3\n";
        var unsortedFileNames = new List<string>();
        var unsortedWriters = new List<InMemoryStreamWriter>();
        var fileSystem = CreateTestFileSystem(sourceContent, unsortedFileNames, unsortedWriters, "\n");

        // Act
        var filePartitioner = new FileSplitter(fileSystem, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSplitter>.Instance);
        var filenames = filePartitioner.SplitFileAsync("sourcePath",  TestContext.Current.CancellationToken);
        var names =  await filenames.ToArrayAsync(TestContext.Current.CancellationToken);
        
        // Assert
        names.Should().HaveCount(3);
        // Verify the content written to each writer
        unsortedWriters[0].Content.Should().Be("Line1\n");
        unsortedWriters[1].Content.Should().Be("Line2\n");
        unsortedWriters[2].Content.Should().Be("Line3\n");

    }
    
    [Fact]
    public async Task SplitFile_SplitsFileTwoCorrectly_WithSingleCharNewline()
    {
        // Arrange
        var tempDir = "TestFiles";
        var options = new ExternalSorterSettings()
        {
            TempDirectory = tempDir,
            BatchFileSize = 4,
        };

        var sourceContent = "Line1\nLine2\nLine3\n";
        var unsortedFileNames = new List<string>();
        var unsortedWriters = new List<InMemoryStreamWriter>();
        var fileSystem = CreateTestFileSystem(sourceContent, unsortedFileNames, unsortedWriters, "\n");

        // Act
        var filePartitioner = new FileSplitter(fileSystem, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSplitter>.Instance);
        var filenames = filePartitioner.SplitFileAsync("sourcePath",  TestContext.Current.CancellationToken);
        var names =  await filenames.ToArrayAsync(TestContext.Current.CancellationToken);
        
        // Assert
        names.Should().HaveCount(3);
        
        // Verify the content written to each writer
        unsortedWriters[0].Content.Should().Be("Line1\n");
        unsortedWriters[1].Content.Should().Be("Line2\n");
        unsortedWriters[2].Content.Should().Be("Line3\n");
    }
    
    [Fact]
    public async Task SplitFile_WithEmptySource_ReturnsNoFiles()
    {
        // Arrange
        var tempDir = "TestFiles";
        var options = new ExternalSorterSettings()
        {
            TempDirectory = tempDir,
            BatchFileSize = 10,
        };

        // Prepare in-memory source reader with empty content
        var sourceContent = "";
        var unsortedFileNames = new List<string>();
        var unsortedWriters = new List<InMemoryStreamWriter>();
        var fileSystem = CreateTestFileSystem(sourceContent, unsortedFileNames, unsortedWriters, "\n");

        // Act
        var filePartitioner = new FileSplitter(fileSystem, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSplitter>.Instance);
        var filenames = filePartitioner.SplitFileAsync("emptySourcePath", TestContext.Current.CancellationToken);
        var names = await filenames.ToArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        names.Should().BeEmpty();
    }
    
    [Fact]
    public async Task SplitFile_HandlesTrailingDataWithoutNewline()
    {
        // Arrange
        var tempDir = "TestFiles";
        var options = new ExternalSorterSettings()
        {
            TempDirectory = tempDir,
            BatchFileSize = 8,
        };
        
        var sourceContent = "Line1\r\nLine2\r\nLast";

        var unsortedFileNames = new List<string>();
        var unsortedWriters  = new List<InMemoryStreamWriter>();

        var fileSystem = CreateTestFileSystem(
            sourceContent,
            unsortedFileNames,
            unsortedWriters,
            "\r\n"
        );

        // Act
        var filePartitioner = new FileSplitter(fileSystem, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSplitter>.Instance);
        var filenames = filePartitioner.SplitFileAsync("sourcePath", TestContext.Current.CancellationToken);
        var names = await filenames.ToArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        // FileSplitter writes data until size limit (8 bytes) is reached, then continues to next newline
        // "Line1\r\n" = 7 bytes (under limit)
        // "Line2\r\n" = 7 more bytes (total 14, over limit) -> splits after this newline
        // First chunk: "Line1\r\nLine2\r\n" (14 bytes)
        // Second chunk: "Last" (no newline, just remaining data)
        names.Should().HaveCount(2);
        
        unsortedWriters[0].Content.Should().Be("Line1\r\nLine2\r\n");
        unsortedWriters[1].Content.Should().Be("Last");
    }
    
    private IFileSystem CreateTestFileSystem(string sourceContent, List<string> unsortedFileNames, List<InMemoryStreamWriter> unsortedWriters, string newSeparatorLine)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var fileReader = Substitute.For<IFileReader>();
        var fileWriter = Substitute.For<IFileWriter>();
        var encoding = new UTF8Encoding(false);
        fileSystem.FileReader.Returns(fileReader);
        fileSystem.FileWriter.Returns(fileWriter);
        fileSystem.Encoding.Returns(encoding);

        fileReader.OpenAsPipeReader(Arg.Any<string>()).Returns(callInfo => 
        {
            // Reset the reader to the beginning for each call
            return PipeReader.Create(new MemoryStream(encoding.GetBytes(sourceContent)));
        });
        
        fileWriter
            .CreateText(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var path = callInfo.ArgAt<string>(0);
                unsortedFileNames.Add(path);

                var writer = new InMemoryStreamWriter(newLineSeparator: newSeparatorLine);
                unsortedWriters.Add(writer);
                return writer;
            });

        return fileSystem;
    }
}