namespace App.ExternalSorter.Benchmark;

using BenchmarkDotNet.Attributes;

/// <summary>
/// Benchmark class for testing merge strategy performance.
/// </summary>
[MemoryDiagnoser]
public class MergeBenchmark
{
    
    private List<List<int>> data = new List<List<int>>();
    
    /// <summary>
    /// Sets up the benchmark data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var list = NumberListGenerator.GenerateRandomList(1000000, -10000, 10000);
        data = BuildRuns(list, 64, Comparer<int>.Default);
    }
    
    // /// <summary>
    // /// Benchmarks the polyphase merge strategy.
    // /// </summary>
    // [Benchmark]
    // public void BenchmarkSort()
    // {
    //     var toSort = new List<List<int>>(data);
    //     var strategy = new PolyPhaseStrategy<int>();
    //     var sort = strategy.Merge(toSort, Comparer<int>.Default).ToArray();
    // }
    // 
    // /// <summary>
    // /// Benchmarks the tournament merge strategy.
    // /// </summary>
    // [Benchmark]
    // public void BenchmarTourkSort()
    // {
    //     var toSort = new List<List<int>>(data);
    //     var strategy = new TournamentMergeStrategy<int>();
    //     var sort = strategy.Merge(toSort, Comparer<int>.Default).ToArray();
    // }
    // 
    // /// <summary>
    // /// Benchmarks the K-way merge strategy.
    // /// </summary>
    // [Benchmark]
    // public void BenchmarkSortKway()
    // {
    //     var toSort = new List<List<int>>(data);
    //     var strategy = new KWayStrategy<int>();
    //     var sort = strategy.Merge(toSort, Comparer<int>.Default).ToArray();
    // }
    
    /// <summary>
    /// Builds sorted runs from input data for benchmarking.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="data">The input data to split into runs.</param>
    /// <param name="chunkSize">The size of each run.</param>
    /// <param name="cmp">The comparer to use for sorting.</param>
    /// <returns>A list of sorted runs.</returns>
    public static List<List<T>> BuildRuns<T>(List<T> data, int chunkSize, IComparer<T> cmp)
    {
        var result = new List<List<T>>();
        for (int i = 0; i < data.Count; i += chunkSize)
        {
            int len = Math.Min(chunkSize, data.Count - i);
            var slice = new List<T>(len);
            for (int j = 0; j < len; j++) 
                slice.Add(data[i + j]);
            slice.Sort(cmp);
            result.Add(slice);          // one run
        }
        return result;
    }
    
}