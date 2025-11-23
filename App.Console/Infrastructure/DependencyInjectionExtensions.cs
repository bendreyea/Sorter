using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace App.Console.Infrastructure;

/// <summary>
/// Extension methods for integrating Microsoft.Extensions.DependencyInjection with System.CommandLine 2.0.
/// 
/// Since System.CommandLine 2.0 removed middleware support (CommandLineBuilder, InvocationContext, BindingContext),
/// this provides an alternative approach using closures to capture the service provider and make it available
/// to command constructors.
/// 
/// Usage:
/// <code>
/// var rootCommand = new RootCommand("My App");
/// 
/// // Configure services - this replaces the middleware approach
/// var serviceProvider = rootCommand.ConfigureServices(services =>
/// {
///     services.AddSingleton&lt;MyService&gt;();
///     services.AddLogging();
/// });
/// 
/// // Add commands - they receive the service provider in their constructor
/// rootCommand.AddCommand&lt;MyCommand&gt;(serviceProvider);
/// 
/// return rootCommand.Parse(args).Invoke();
/// </code>
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Configure the root command with dependency injection support.
    /// This creates a service provider that is accessible to all command handlers via closures.
    /// This replaces the UseDependencyInjection middleware from System.CommandLine beta versions.
    /// </summary>
    /// <param name="rootCommand">The root command to configure.</param>
    /// <param name="configureServices">Action to configure services.</param>
    /// <returns>The configured service provider that can be passed to AddCommand.</returns>
    public static IServiceProvider ConfigureServices(
        this RootCommand rootCommand,
        Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        configureServices(services);
        return services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Add a command with dependency injection support.
    /// The command will receive the service provider in its constructor via ActivatorUtilities,
    /// allowing full constructor injection of services.
    /// </summary>
    /// <typeparam name="TCommand">The command type that extends System.CommandLine.Command.</typeparam>
    /// <param name="rootCommand">The root command to add the subcommand to.</param>
    /// <param name="serviceProvider">The service provider created by ConfigureServices.</param>
    public static void AddCommand<TCommand>(
        this RootCommand rootCommand,
        IServiceProvider serviceProvider) where TCommand : Command
    {
        var command = (TCommand)ActivatorUtilities.CreateInstance(serviceProvider, typeof(TCommand), serviceProvider);
        rootCommand.Subcommands.Add(command);
    }
}
