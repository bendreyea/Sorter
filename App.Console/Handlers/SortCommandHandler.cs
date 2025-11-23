namespace App.Console.Handlers;

using Commands.Sort;
using ExternalSorter.Core;
using Infrastructure;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles the sort command execution.
/// </summary>
public class SortCommandHandler : ICommandOptionsHandler<SortCommandOptions>
{
    private readonly Sorter _sorter;
    private readonly ILogger<SortCommandHandler> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SortCommandHandler"/> class.
    /// </summary>
    /// <param name="sorter">The sorter.</param>
    /// <param name="logger">The logger.</param>
    public SortCommandHandler(Sorter sorter, ILogger<SortCommandHandler> logger)
    {
        _sorter = sorter;
        _logger = logger;
    }

    /// <summary>
    /// Handles the sort command asynchronously.
    /// </summary>
    /// <param name="options">The command options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> HandleAsync(SortCommandOptions options, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting sort...");
            await _sorter.Sort(options.InputFileName, options.OutputFileName, cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sort");
            return 1;
        }
        
        _logger.LogInformation($"Sorting file '{options.InputFileName}' to '{options.OutputFileName}'");
        
        return 0;
    }
}