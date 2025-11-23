namespace App.Generator;

/// <summary>
/// Represents a distribution of random numbers.
/// </summary>
public interface IDistribution
{
    /// <summary>
    /// Generates a random number.
    /// </summary>
    int Generate(IRandom rand);
}