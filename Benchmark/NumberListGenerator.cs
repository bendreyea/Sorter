namespace App.ExternalSorter.Benchmark;

/// <summary>
/// Utility class for generating lists of numbers for benchmarking purposes.
/// </summary>
public static class NumberListGenerator
{
    private static readonly Random _rng = new Random();

    /// <summary>
    /// Generates a list of integers containing sequential integers 
    /// from start (inclusive) up to start + count − 1.
    /// </summary>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of elements to generate.</param>
    /// <returns>A list of sequential integers.</returns>
    public static List<int> GenerateSequentialList(int start, int count)
    {
        if (count < 0) throw new ArgumentException("count must be non‐negative", nameof(count));

        var result = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(start + i);
        }
        return result;
    }

    /// <summary>
    /// Generates a list of integers of the given length, 
    /// where each element is a random integer in [minValue, maxValue].
    /// </summary>
    /// <param name="count">The number of elements to generate.</param>
    /// <param name="minValue">The minimum value (inclusive).</param>
    /// <param name="maxValue">The maximum value (inclusive).</param>
    /// <returns>A list of random integers.</returns>
    public static List<int> GenerateRandomList(int count, int minValue = 0, int maxValue = 100)
    {
        if (count < 0) 
            throw new ArgumentException("count must be non‐negative", nameof(count));
        if (minValue > maxValue) 
            throw new ArgumentException("minValue must be <= maxValue");

        var result = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(_rng.Next(minValue, maxValue + 1));
        }
        return result;
    }  
}