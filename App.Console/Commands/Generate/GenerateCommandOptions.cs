namespace App.Console.Commands.Generate;

using Infrastructure;

/// <summary>
/// Options for the generate command.
/// </summary>
public class GenerateCommandOptions : ICommandOptions
{
    /// <summary>
    /// The name of generated file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// The file size in megabytes.
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Optional output directory.
    /// </summary>
    public string? OutputDir { get; set; }
}