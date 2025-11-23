namespace App.Generator;

/// <summary>
/// Represents a random number generator.
/// </summary>
public interface IRandom : IDisposable
{
    /// <summary>
    /// Returns a non-negative random integer.
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Returns a random integer that is within a specified range.
    /// </summary>
    int Next(int minValue, int maxValue);
}