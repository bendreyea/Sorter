namespace App.ExternalSorter.UnitTests;

using App.ExternalSorter.Configuration;
using App.ExternalSorter.Merge.PolyPhase;
using App.ExternalSorter.Sorting;
using App.ExternalSorter.UnitTests.TestData;
using App.FileSystem.InMemory;
using App.FileSystem.Interfaces;
using NSubstitute;

public class PolyPhaseStrategyFilesTests
{
    private static readonly IComparer<string> Comparer = new StringAndNumberComparer();
     
     [Theory]
     [MemberData(nameof(SortingTestData.GetSortingTestCases), MemberType = typeof(SortingTestData))]
     public async Task MergeFiles_MergesSortedFilesCorrectly(string[] cases, string[] expectedResult)
     {
         // Arrange
         var fileSystem = Substitute.For<IFileSystem>();
         var fileReader = Substitute.For<IFileReader>();
         var fileWriter = Substitute.For<IFileWriter>();

         fileSystem.FileReader.Returns(fileReader);
         fileSystem.FileWriter.Returns(fileWriter);

         var counter = 0;
         // Create sorted input files content
         var sortedFileContents = new Dictionary<string, List<string>>();
         var splitCases = cases.AsReadOnly().SplitList(3);
         var sortedFiles = new List<string>();
         foreach (var collection in splitCases)
         {
             var filename = $"sorted{++counter}.txt";
             collection.Sort(Comparer);
             sortedFileContents[filename] = collection;
             sortedFiles.Add(filename);
         }

         // Dictionary to keep track of in-memory writers
         var inMemoryWriters = new Dictionary<string, InMemoryStreamWriter>();

         // Set up fileWriter.CreateText to return InMemoryStreamWriter
         fileWriter.CreateText(Arg.Any<string>()).Returns(callInfo =>
         {
             var path = callInfo.ArgAt<string>(0);
             var writer = new InMemoryStreamWriter();
             inMemoryWriters[path] = writer;
             return writer;
         });

         // Set up fileReader.OpenText to return InMemoryStreamReader
         fileReader.OpenText(Arg.Any<string>()).Returns(callInfo =>
         {
             var path = callInfo.ArgAt<string>(0);
             if (inMemoryWriters.TryGetValue(path, out var writer))
             {
                 // Reading from a file we wrote to earlier
                 var content = writer.Content;
                 var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                 return new InMemoryStreamReader(lines, Environment.NewLine);
             }
             else
             {
                 // Reading from initial sorted files
                 var fileName = System.IO.Path.GetFileName(path);
                 if (sortedFileContents.TryGetValue(fileName, out var lines))
                 {
                     return new InMemoryStreamReader(lines, Environment.NewLine);
                 }
                 else
                 {
                     throw new FileNotFoundException($"File not found: {path}");
                 }
             }
         });

         // Handle MoveFile
         fileSystem.When(fs => fs.MoveFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())).Do(callInfo =>
         {
             var source = callInfo.ArgAt<string>(0);
             var destination = callInfo.ArgAt<string>(1);

             if (inMemoryWriters.ContainsKey(source))
             {
                 inMemoryWriters[destination] = inMemoryWriters[source];
                 inMemoryWriters.Remove(source);
             }
         });

         // Handle DeleteFile
         fileSystem.When(fs => fs.DeleteFile(Arg.Any<string>())).Do(callInfo =>
         {
             var path = callInfo.ArgAt<string>(0);
             inMemoryWriters.Remove(path);
         });

         var mergerOptions = new ExternalSorterSettings()
         {
         };

         var mergeProcessor = new PolyPhaseStrategyFiles(fileSystem, mergerOptions, Microsoft.Extensions.Logging.Abstractions.NullLogger<PolyPhaseStrategyFiles>.Instance);
         
         // Act
         var final = await mergeProcessor.Merge(sortedFiles, Comparer, CancellationToken.None);

         // Assert
         // Verify the final file exists in our in-memory file system
         Assert.True(inMemoryWriters.ContainsKey(final), $"Final merged file '{final}' should exist");

         var mergedContent = inMemoryWriters[final].Content;
         var mergedLines = mergedContent.Split(new[] {  "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
         

         Assert.Equal(expectedResult.Length, mergedLines.Length);
         for (var i = 0; i < expectedResult.Length; i++)
         {
             Assert.Equal(expectedResult[i], mergedLines[i]);
         }

         // Verify that DeleteFile was called for the initial sorted files
         // Note: PolyPhaseStrategyFiles does NOT delete the input files, only intermediate temp files.
         // The caller (Sorter.cs) is responsible for deleting the input chunks.
         foreach (var fileName in sortedFiles)
         {
             await fileSystem.DidNotReceive().DeleteFileAsync(fileName, Arg.Any<CancellationToken>());
         }
     }
}