namespace App.ExternalSorter.Sorting;

using System.Runtime.CompilerServices;

/// <summary>
/// Preprocessed representation of a string for efficient sorting.
/// Parses the string once and caches comparison data.
/// 
/// For "123. Hello World":
/// - Number: 123
/// - TextStart: 5 (position after ". ")
/// - TextLength: 11
/// </summary>
public readonly struct SortKey : IComparable<SortKey>
{
    private readonly long Number; // Parsed numeric prefix (using long to handle large numbers)
    private readonly ReadOnlyMemory<char> OriginalData; // Reference to original data
    private readonly int TextStart; // Start position of text part
    private readonly int TextLength; // Length of text part

    /// <summary>
    /// Gets the original string value.
    /// </summary>
    public ReadOnlyMemory<char> Value => OriginalData;

    /// <summary>
    /// Gets the original index of the item in the array.
    /// </summary>
    public readonly int OriginalIndex; // Original position in array
    

    /// <summary>
    /// Initializes a new instance of the <see cref="SortKey"/> struct.
    /// </summary>
    /// <param name="value">The memory value.</param>
    /// <param name="index">The original index.</param>
    public SortKey(ReadOnlyMemory<char> value, int index)
    {
        OriginalIndex = index;
        OriginalData = value;

        ReadOnlySpan<char> span = value.Span;
        int dotIndex = span.IndexOf('.');

        if (dotIndex > 0)
        {
            // Parse number part (before the dot)
            Number = ParseNumber(span.Slice(0, dotIndex));

            // Text starts after ". " (dot + space)
            TextStart = dotIndex + 1;
            while (TextStart < span.Length && span[TextStart] == ' ')
            {
                TextStart++;
            }
            TextLength = Math.Max(0, span.Length - TextStart);
        }
        else
        {
            // No number prefix, treat entire string as text
            Number = 0;
            TextStart = 0;
            TextLength = span.Length;
        }
    }

    /// <summary>
    /// Parses integer from span without allocations.
    /// Handles negative numbers and large numbers up to long.MaxValue.
    /// Falls back to uint parsing if int parsing fails (to match StringAndNumberComparer behavior).
    /// </summary>
    private static long ParseNumber(ReadOnlySpan<char> span)
    {
        if (span.Length == 0)
            return 0;
            
        // Try int parsing first
        if (int.TryParse(span, out int intValue))
            return intValue;
        
        // Fall back to uint parsing for large positive numbers
        if (uint.TryParse(span, out uint uintValue))
            return uintValue;
        
        // If both fail, return 0
        return 0;
    }

    /// <summary>
    /// Compares two SortKeys using the following order:
    /// 1. Case-insensitive text comparison (ignoring leading/trailing spaces)
    /// 2. Case-sensitive comparison (lowercase before uppercase)  
    /// 3. Number comparison
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(SortKey other)
    {
        // Get text spans without creating new strings
        ReadOnlySpan<char> text1 = OriginalData.Span.Slice(TextStart, TextLength);
        ReadOnlySpan<char> text2 = other.OriginalData.Span.Slice(other.TextStart, other.TextLength);

        // Case-insensitive comparison
        int caseInsensitiveResult = text1.CompareTo(text2, StringComparison.OrdinalIgnoreCase);
        if (caseInsensitiveResult != 0)
            return caseInsensitiveResult;

        // Text is equal case-insensitively, now do case-sensitive comparison
        // Lowercase should come before uppercase
        int minLen = Math.Min(text1.Length, text2.Length);
        for (int i = 0; i < minLen; i++)
        {
            char c1 = text1[i];
            char c2 = text2[i];
            
            if (c1 != c2)
            {
                // If characters differ in case only
                char c1Lower = char.ToLowerInvariant(c1);
                char c2Lower = char.ToLowerInvariant(c2);
                
                if (c1Lower == c2Lower)
                {
                    // Same letter, different case - lowercase comes first
                    // lowercase has higher ASCII value than uppercase, so invert
                    return c2.CompareTo(c1);
                }
            }
        }

        // Exact same text (same case), compare by number
        return Number.CompareTo(other.Number);
    }
}