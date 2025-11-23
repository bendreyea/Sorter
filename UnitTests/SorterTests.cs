namespace App.ExternalSorter.UnitTests;

using App.ExternalSorter.Core;
using App.ExternalSorter.Sorting;
using App.ExternalSorter.UnitTests.TestData;
using FluentAssertions;

public class SorterTests
{

    private readonly IComparer<string> _comparer = new StringAndNumberComparer();

    [Theory]
    [MemberData(nameof(SortingTestData.GetSortingTestCases), MemberType = typeof(SortingTestData))]
    public void ThreeWayRadixQuicksort_SortedArrayCorrectly(string[] cases, string[] expectedCases)
    {
        // Perform the sort using three-way radix quicksort
        ThreeWayRadixQuickSort.Sort(cases);
        cases.Should().Equal(expectedCases);
    }

    [Theory]
    [MemberData(nameof(SortingTestData.GetSortingTestCases), MemberType = typeof(SortingTestData))]
    public void ThreeWayRadixQuicksortAndQuicksort_ShouldSortCorrectly(string[] cases, string[] expectedCases)
    {
        var copyCases = (string[])cases.Clone();
        ThreeWayRadixQuickSort.Sort(cases);
        Array.Sort(copyCases, _comparer); 
        cases.Should().Equal(copyCases);
        copyCases.Should().Equal(expectedCases);
    }
    
    [Theory]
    [MemberData(nameof(SortingTestData.GetSortingTestCases), MemberType = typeof(SortingTestData))]
    public void Quicksort_ShouldSortCorrectly(string[] cases, string[] expectedCases)
    {
        Array.Sort(cases, _comparer); 
        cases.Should().Equal(expectedCases);
    }
}