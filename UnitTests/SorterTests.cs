namespace App.ExternalSorter.UnitTests;

using App.ExternalSorter.Sorting;
using App.ExternalSorter.UnitTests.TestData;
using FluentAssertions;

public class SorterTests
{

    private readonly IComparer<string> _comparer = new StringAndNumberComparer();
    
    [Theory]
    [MemberData(nameof(SortingTestData.GetSortingTestCases), MemberType = typeof(SortingTestData))]
    public void Quicksort_ShouldSortCorrectly(string[] cases, string[] expectedCases)
    {
        Array.Sort(cases, _comparer); 
        cases.Should().Equal(expectedCases);
    }
}