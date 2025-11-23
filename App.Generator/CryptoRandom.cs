namespace App.Generator;

using System.Security.Cryptography;

/// <summary>
/// Provides a random number generator that uses a cryptographically secure random number generator.
/// </summary>
public class CryptoRandom : IRandom
{
    private readonly RandomNumberGenerator _rng;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoRandom"/> class.
    /// </summary>
    public CryptoRandom()
    {
        _rng = RandomNumberGenerator.Create();
    }

    /// <summary>
    /// Generates a random double value between 0.0 and 1.0.
    /// </summary>
    /// <returns>A random double value.</returns>
    public double NextDouble()
    {
        // Generate 8 bytes (64 bits)
        var bytes = new byte[8];
        _rng.GetBytes(bytes);

        // Convert bytes to UInt64
        ulong uint64 = BitConverter.ToUInt64(bytes, 0);

        // Scale to [0.0, 1.0)
        return uint64 / (double)(ulong.MaxValue + 1.0);
    }

    /// <summary>
    /// Generates a random integer between minValue and maxValue.
    /// </summary>
    /// <param name="minValue">The minimum value.</param>
    /// <param name="maxValue">The maximum value.</param>
    /// <returns>A random integer.</returns>
    public int Next(int minValue, int maxValue)
    {
        return RandomNumberGenerator.GetInt32(minValue, maxValue);
    }

    /// <summary>
    /// Disposes the random number generator.
    /// </summary>
    public void Dispose()
    {
        _rng.Dispose();
    }
}