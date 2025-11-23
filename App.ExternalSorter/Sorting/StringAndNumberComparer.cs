namespace App.ExternalSorter.Sorting;

/// <summary>
/// Compares two strings that contain both numeric and alphabetic parts.
/// </summary>
public class StringAndNumberComparer : IComparer<string>
{
    /// <summary>
    /// Compares two strings containing numeric and alphabetic parts.
    /// </summary>
    /// <param name="x">The first string to compare.</param>
    /// <param name="y">The second string to compare.</param>
    /// <returns>A value indicating the relative order of the strings.</returns>
    public int Compare(string? x, string? y)
    {
        // Handle null cases (nulls are considered "less" than any string)
        if (x == null && y == null) 
            return 0;
        
        if (x == null) 
            return -1;
        
        if (y == null) 
            return 1;

        // Get the number and string part indices for both x and y
        GetNumberAndStringIndices(x.AsSpan(), out int xNumberEnd, out int xStringStart);
        GetNumberAndStringIndices(y.AsSpan(), out int yNumberEnd, out int yStringStart);

        // Compare the string parts first (case-insensitive comparison using Span<char>)
        ReadOnlySpan<char> xSpan = x.AsSpan(xStringStart);
        ReadOnlySpan<char> ySpan = y.AsSpan(yStringStart);
        // Compare text parts (case-insensitive)
        int textCompare = xSpan.CompareTo(ySpan, StringComparison.OrdinalIgnoreCase);
        if (textCompare != 0)
        {
            return textCompare;
        }

        // Text parts are equal case-insensitively; compare case-sensitively
        textCompare = xSpan.CompareTo(ySpan, StringComparison.Ordinal);
        if (textCompare != 0)
        {
            // Invert the result to have lowercase come before uppercase
            return -textCompare;
        }

        // If the string parts are equal, compare the numeric parts
        ReadOnlySpan<char> xNumberSpan = x.AsSpan(0, xNumberEnd);
        ReadOnlySpan<char> yNumberSpan = y.AsSpan(0, yNumberEnd);
        if (int.TryParse(xNumberSpan, out int xNumber) && int.TryParse(yNumberSpan, out int yNumber))
        {
            return xNumber.CompareTo(yNumber);
        }

        if (uint.TryParse(xNumberSpan, out uint xUInt) && uint.TryParse(yNumberSpan, out uint yUInt))
        {
            return xUInt.CompareTo(yUInt);
        }
        
        // In case parsing fails, treat the number as 0
        return 0;
    }

    private static void GetNumberAndStringIndices(ReadOnlySpan<char> span, out int numberEnd, out int stringStart)
    {
        // Find the first occurrence of the period (.)
        int periodIndex = span.IndexOf('.');

        // If there's no period, treat the entire string as a numberless part
        if (periodIndex == -1)
        {
            numberEnd = 0;
            stringStart = 0;
            return;
        }

        // Parse the indices: number part ends at the period, string part starts after period and spaces
        numberEnd = periodIndex;
        stringStart = periodIndex + 1;

        // Skip spaces after the period
        while (stringStart < span.Length && span[stringStart] == ' ')
        {
            stringStart++;
        }
    }
}