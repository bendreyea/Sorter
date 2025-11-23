namespace App.Console.UnitTests;

using System.CommandLine;
using Commands.Generate;
using Commands.Sort;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

public class CommandLineTests
{
    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void GenerateCommand_ShouldHaveCorrectName()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var command = new GenerateCommand(serviceProvider);

        // Assert
        command.Name.Should().Be("generate");
    }

    [Fact]
    public void GenerateCommand_ShouldHave3Options()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var command = new GenerateCommand(serviceProvider);

        // Assert
        command.Options.Should().HaveCount(3);
    }

    [Fact]
    public void SortCommand_ShouldHaveCorrectName()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var command = new SortCommand(serviceProvider);

        // Assert
        command.Name.Should().Be("sort");
    }

    [Fact]
    public void SortCommand_ShouldHave2Options()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var command = new SortCommand(serviceProvider);

        // Assert
        command.Options.Should().HaveCount(2);
    }

    [Fact]
    public void RootCommand_ShouldParseGenerateCommandCorrectly()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var rootCommand = new RootCommand("External Sorter - Generate and sort large files");
        rootCommand.Subcommands.Add(new GenerateCommand(serviceProvider));
        rootCommand.Subcommands.Add(new SortCommand(serviceProvider));

        // Act
        var parseResult = rootCommand.Parse("generate --file-name test.txt --file-size 100");

        // Assert
        parseResult.Errors.Should().BeEmpty();
        parseResult.CommandResult.Command.Name.Should().Be("generate");
    }

    [Fact]
    public void RootCommand_ShouldParseSortCommandCorrectly()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var rootCommand = new RootCommand("External Sorter - Generate and sort large files");
        rootCommand.Subcommands.Add(new GenerateCommand(serviceProvider));
        rootCommand.Subcommands.Add(new SortCommand(serviceProvider));

        // Act
        var parseResult = rootCommand.Parse("sort --input input.txt --output output.txt");

        // Assert
        parseResult.Errors.Should().BeEmpty();
        parseResult.CommandResult.Command.Name.Should().Be("sort");
    }
}
