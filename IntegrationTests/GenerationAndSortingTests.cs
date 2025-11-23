// namespace App.ExternalSorter.IntegrationTests;
//
// using System.Text;
// using Sorting;
// using Sorting.Models;
// using FluentAssertions;
// using FileSystem;
// using FileSystem.Wrappers;
// using Generator;
// using SortStrategies.Comparers;
//
// public class GenerationAndSortingTests : IAsyncLifetime
// {
//     private readonly FileGenerator _fileGenerator;
//     private readonly ExternalMergeSorter _sorter;
//     private const string UnsortedSource = "unsorted.txt";
//     private const string Sorted = "sorted.txt";
//     private const string Sorted2 = "sortedpolyphase.txt";
//     private readonly IFileSystem _fileSystem;
//     private readonly IComparer<string> _comparer = new StringAndNumberComparer();
//     private readonly string _dirLocation;
//
//     public GenerationAndSortingTests()
//     {
//         Encoding encoding = Encoding.UTF8;
//         _fileSystem = new FileSystem(encoding);
//         _fileGenerator = new FileGenerator(_fileSystem);
//         var uuid = Guid.NewGuid().ToString();
//         _dirLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, uuid);
//         if (!Directory.Exists(_dirLocation))
//         {
//             Directory.CreateDirectory(_dirLocation);
//         }
//         _sorter = new ExternalMergeSorter(_fileSystem, new MergeSortOptions()
//         {
//             SortOptions = new SortOptions()
//             {
//                 FileLocation = _dirLocation,
//                 FilesPerRun = 2
//             },
//             MergeProcessorOptions = new MergeProcessorOptions()
//             {
//                 FileLocation = _dirLocation,
//                 FilesPerRun = 4
//             },
//             FilePartitionerOptions = new FilePartitionerOptions()
//             {
//                 FileLocation = _dirLocation,
//                 FileSize = 1024
//             }
//         });
//     }
//     
//     [Fact]
//     public async Task MergeSort_ShouldSortFileInCorrectOrder()
//     {
//         var sortedInMemoryList = await ReadAllLines(Path.Combine(_dirLocation, UnsortedSource));
//         sortedInMemoryList.Sort(_comparer);
//         await _sorter.Sort(Path.Combine(_dirLocation, UnsortedSource), Path.Combine(_dirLocation, Sorted), default);
//         var mergeSortedList = await ReadAllLines(Path.Combine(_dirLocation, Sorted));
//         mergeSortedList.Should().Equal(sortedInMemoryList);
//     }
//
//     public Task InitializeAsync()
//     {
//         return _fileGenerator.GenerateFile(Path.Combine(_dirLocation, UnsortedSource), 1*1024*1024, default);
//     }
//
//     public Task DisposeAsync()
//     {
//         _fileSystem.DeleteFile(Path.Combine(_dirLocation,UnsortedSource));
//         _fileSystem.DeleteFile(Path.Combine(_dirLocation,Sorted));
//         _fileSystem.DeleteFile(Path.Combine(_dirLocation,Sorted2));
//         
//         Directory.Delete(_dirLocation);
//         
//         return Task.CompletedTask;
//     }
//
//     private async Task<List<string>> ReadAllLines(string path)
//     {
//         using var streamReader = _fileSystem.FileReader.OpenText(path);
//         var lines = new List<string>();
//         while (!streamReader.EndOfStream)
//         {
//             lines.Add((await streamReader.ReadLineAsync())!);
//         }
//
//         return lines;
//     }
// }