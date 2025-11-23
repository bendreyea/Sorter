using App.Console.Commands;
using App.Generator;
using System.CommandLine;
using Console = System.Console;

namespace App.Console.Handlers;

using Commands.Generate;
using Infrastructure;

/// <summary>
/// Handles the generate command execution.
/// </summary>
public class GenerateCommandHandler : ICommandOptionsHandler<GenerateCommandOptions>
{
    private readonly FileGenerator _fileGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateCommandHandler"/> class.
    /// </summary>
    /// <param name="fileGenerator">The file generator.</param>
    public GenerateCommandHandler(FileGenerator fileGenerator)
    {
        _fileGenerator = fileGenerator;
    }

    /// <summary>
    /// Handles the generate command asynchronously.
    /// </summary>
    /// <param name="options">The command options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> HandleAsync(GenerateCommandOptions options, CancellationToken cancellationToken)
    {
        // Convert the file size from megabytes to bytes
        var fileSizeInBytes = options.FileSize * 1024 * 1024;
        
        // Combine output directory with filename if provided
        var outputPath = string.IsNullOrWhiteSpace(options.OutputDir) 
            ? options.FileName 
            : Path.Combine(options.OutputDir, options.FileName);
        
        System.Console.WriteLine($"Generating file '{outputPath}' with size {fileSizeInBytes} bytes.");
        
        await _fileGenerator.GenerateFile(outputPath, fileSizeInBytes, cancellationToken);
        
        System.Console.WriteLine($"File generated successfully at: {Path.GetFullPath(outputPath)}");
        
        return 0;
    }
}