namespace App.Console.Commands.Sort;

using System.CommandLine;
using App.Console.Handlers;
using Infrastructure;

/// <summary>
/// Command for sorting files using external sorting algorithm.
/// </summary>
public class SortCommand : CommandBase<SortCommandOptions, SortCommandHandler>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SortCommand"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public SortCommand(IServiceProvider serviceProvider) 
        : base("sort", "Sort a file using external sorting algorithm", serviceProvider)
    {
        var inputOption = new Option<string>("--input")
        {
            Description = "The input file to sort",
            Arity = ArgumentArity.ExactlyOne
        };
        inputOption.Aliases.Add("-i");

        var outputOption = new Option<string>("--output")
        {
            Description = "The output file for sorted data",
            Arity = ArgumentArity.ExactlyOne
        };
        outputOption.Aliases.Add("-o");

        RegisterOption(inputOption, (opts, value) => opts.InputFileName = value ?? string.Empty);
        RegisterOption(outputOption, (opts, value) => opts.OutputFileName = value ?? string.Empty);
        
        SetupHandler();
    }
}