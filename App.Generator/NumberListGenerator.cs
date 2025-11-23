namespace App.Generator;

/// <summary>
/// Provides methods for generating number lists.
/// </summary>
public static class NumberListGenerator
{
    private static readonly Random Rng = new Random();

    /// <summary>
    /// Generates a List int containing sequential integers 
    /// from start (inclusive) up to start + count − 1.
    /// </summary>
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
    /// Generates a List int of the given length, 
    /// where each element is a random integer in [minValue, maxValue].
    /// </summary>
    public static List<int> GenerateRandomList(int count, int minValue = 0, int maxValue = 100)
    {
        if (count < 0) 
            throw new ArgumentException("count must be non‐negative", nameof(count));
        if (minValue > maxValue) 
            throw new ArgumentException("minValue must be <= maxValue");

        var result = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            result.Add(Rng.Next(minValue, maxValue + 1));
        }
        return result;
    }  
}