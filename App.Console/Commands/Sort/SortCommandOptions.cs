namespace App.Console.Commands.Sort;

using Infrastructure;

/// <summary>
/// Options for the sort command.
/// </summary>
public class SortCommandOptions : ICommandOptions
{
    /// <summary>
    /// The input file name to sort.
    /// </summary>
    public string InputFileName { get; set; } = string.Empty;
    
    /// <summary>
    /// The output file name for sorted data.
    /// </summary>
    public string OutputFileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional output directory.
    /// </summary>
    public string? OutputDir { get; set; }
}