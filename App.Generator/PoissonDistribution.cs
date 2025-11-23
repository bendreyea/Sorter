namespace App.Generator;

/// <summary>
/// Represents a Poisson distribution.
/// </summary>
public class PoissonDistribution : IDistribution
{
    private double _lambda;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoissonDistribution"/> class.
    /// </summary>
    /// <param name="lambda">The lambda parameter for the Poisson distribution.</param>
    public PoissonDistribution(double lambda)
    {
        _lambda = lambda;
    }

    /// <summary>
    /// Generates a random number from the Poisson distribution.
    /// </summary>
    /// <param name="rand">The random number generator.</param>
    /// <returns>A random number from the distribution.</returns>
    public int Generate(IRandom rand)
    {
        var L = Math.Exp(-_lambda);
        var k = 0;
        var p = 1.0;
        do
        {
            k++;
            var u = rand.NextDouble();
            p *= u;
        }
        while (p > L);

        // Ensure at least 1
        return Math.Max(k - 1, 1);
    }
}