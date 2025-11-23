namespace App.Console.Commands.Generate;

using System.CommandLine;
using App.Console.Handlers;
using Infrastructure;

/// <summary>
/// Command for generating test files with random data.
/// </summary>
public class GenerateCommand : CommandBase<GenerateCommandOptions, GenerateCommandHandler>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateCommand"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public GenerateCommand(IServiceProvider serviceProvider) 
        : base("generate", "Generate a test file with random data", serviceProvider)
    {
        var fileNameOption = new Option<string>("--file-name")
        {
            Description = "The name of the file to generate",
            Arity = ArgumentArity.ExactlyOne
        };
        fileNameOption.Aliases.Add("-f");

        var fileSizeOption = new Option<long>("--file-size")
        {
            Description = "The size of the file in megabytes",
            Arity = ArgumentArity.ExactlyOne
        };
        fileSizeOption.Aliases.Add("-s");

        var outputDirOption = new Option<string?>("--output-dir")
        {
            Description = "The output directory (optional)"
        };
        outputDirOption.Aliases.Add("-o");

        RegisterOption(fileNameOption, (opts, value) => opts.FileName = value ?? string.Empty);
        RegisterOption(fileSizeOption, (opts, value) => opts.FileSize = value);
        RegisterOption(outputDirOption, (opts, value) => opts.OutputDir = value);
        
        SetupHandler();
    }
}