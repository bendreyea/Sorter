namespace App.ExternalSorter.UnitTests;

using App.ExternalSorter.Merge.PolyPhase;
using App.ExternalSorter.Merge.Tournament;
using App.ExternalSorter.Sorting;
using App.ExternalSorter.UnitTests.TestData;
using FluentAssertions;

public class MergeTests
{
    [Theory]
    [MemberData(nameof(SortingTestData.GetSortingTestCases), MemberType = typeof(SortingTestData))]
    public void PolyphaseMerge_Should_SortArrayCorrectly(string[] unsorted, string[] expected)
    {
         var comparer = new StringAndNumberComparer();
         var strategy = new PolyPhaseStrategy<string>();
         var runs = SplitIntoRuns(unsorted, 3);
         var result = strategy.Merge(runs, comparer).ToArray();
         result.Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(SortingTestData.GetSortingTestCases), MemberType = typeof(SortingTestData))]
    public void TournamentMerge_Should_SortArrayCorrectly(string[] unsorted, string[] expected)
    {
        var comparer = new StringAndNumberComparer();
        var strategy = new TournamentMergeStrategy<string>();
        var runs = SplitIntoRuns(unsorted, 3);
        var result = strategy.Merge(runs, comparer).ToArray();
        result.Should().Equal(expected);
    }

    private List<List<T>> SplitIntoRuns<T>(T[] data, int runSize)
    {
        var comparer = typeof(T) == typeof(string) 
            ? (IComparer<T>)(object)new StringAndNumberComparer()
            : Comparer<T>.Default;
            
        var runs = new List<List<T>>();
        for (int i = 0; i < data.Length; i += runSize)
        {
            var run = new List<T>();
            for (int j = i; j < i + runSize && j < data.Length; j++)
            {
                run.Add(data[j]);
            }
            run.Sort(comparer);
            runs.Add(run);
        }
        
        return runs;
    }
}