namespace App.Console.Infrastructure;

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Marker interface for command options.
/// </summary>
public interface ICommandOptions
{
}

/// <summary>
/// Interface for command options handlers.
/// </summary>
/// <typeparam name="TOptions">The type of options.</typeparam>
public interface ICommandOptionsHandler<in TOptions>
{
    /// <summary>
    /// Handles the command asynchronously.
    /// </summary>
    /// <param name="options">The command options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    Task<int> HandleAsync(TOptions options, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for commands with options and handlers.
/// </summary>
/// <typeparam name="TOptions">The type of options.</typeparam>
/// <typeparam name="TOptionsHandler">The type of options handler.</typeparam>
public abstract class CommandBase<TOptions, TOptionsHandler> : Command
    where TOptions : class, ICommandOptions, new()
    where TOptionsHandler : class, ICommandOptionsHandler<TOptions>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<Action<ParseResult, TOptions>> _optionBinders = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBase{TOptions, TOptionsHandler}"/> class.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="description">The command description.</param>
    /// <param name="serviceProvider">The service provider.</param>
    protected CommandBase(string name, string description, IServiceProvider serviceProvider)
        : base(name, description)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers an option with the command.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="option">The option to register.</param>
    /// <param name="setter">The setter action to bind the option value to options.</param>
    protected void RegisterOption<T>(Option<T> option, Action<TOptions, T?> setter)
    {
        this.Options.Add(option);
        
        // Create a binding action that captures the option and setter
        _optionBinders.Add((parseResult, options) =>
        {
            var value = parseResult.GetValue(option);
            setter(options, value);
        });
    }

    /// <summary>
    /// Sets up the command handler.
    /// </summary>
    protected void SetupHandler()
    {
        this.SetAction(async (parseResult) => await HandleCommand(parseResult));
    }

    private async Task<int> HandleCommand(ParseResult parseResult)
    {
        var options = new TOptions();
        
        // Apply all registered option bindings
        foreach (var binder in _optionBinders)
        {
            binder(parseResult, options);
        }

        var handler = ActivatorUtilities.CreateInstance<TOptionsHandler>(_serviceProvider);
        return await handler.HandleAsync(options, CancellationToken.None);
    }
}