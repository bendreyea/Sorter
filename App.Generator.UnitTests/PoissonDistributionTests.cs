namespace App.Generator.UnitTests;

using FluentAssertions;
using NSubstitute;

public class PoissonDistributionTests
{
    [Fact]
    public void Generate_ShouldReturnValidPoissonDistributionValue()
    {
        // Arrange
        var lambda = 3.0;
        var poisson = new PoissonDistribution(lambda);
        var random = Substitute.For<IRandom>();

        // return values that make the loop terminate after k=2 iterations
        random.NextDouble().Returns(0.5, 0.3, 0.1);

        // Act
        var result = poisson.Generate(random);

        // Assert
        // Expecting k-1 = 2 because loop should stop after two iterations
        result.Should().Be(2);
    }

    [Fact]
    public void Generate_ShouldReturnHigherValueForHigherLambda()
    {
        // Arrange
        var lambda = 10.0;
        var poisson = new PoissonDistribution(lambda);
        var random = Substitute.For<IRandom>();

        // Mock random values that simulate a higher number of iterations due to higher lambda
        random.NextDouble().Returns(0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1);

        // Act
        var result = poisson.Generate(random);

        // Assert
        // For a higher lambda, result should generally be greater than 1
        result.Should().BeGreaterThan(1);
    }

    [Fact]
    public void Generate_ShouldStopWhenProductLessThanThreshold()
    {
        // Arrange
        var lambda = 2.0;
        var poisson = new PoissonDistribution(lambda);
        var random = Substitute.For<IRandom>();

        // Product will become less than L after 3 iterations
        random.NextDouble().Returns(0.9, 0.8, 0.05);

        // Act
        var result = poisson.Generate(random);

        // Assert
        // Expecting k-1 = 2 because loop should stop after three iterations
        result.Should().Be(2);
    }
}