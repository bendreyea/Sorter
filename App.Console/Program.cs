using System.CommandLine;
using System.Diagnostics;
using System.Text;
using App.Console.Commands.Generate;
using App.Console.Commands.Sort;
using App.Console.Infrastructure;
using App.ExternalSorter.Configuration;
using App.ExternalSorter.Core;
using App.ExternalSorter.Merge;
using App.ExternalSorter.Merge.PolyPhase;
using App.FileSystem.Implementations;
using App.FileSystem.Interfaces;
using App.Generator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Create root command
var rootCommand = new RootCommand("External Sorter - Generate and sort large files");

// Configure services using DI extension method
var serviceProvider = rootCommand.ConfigureServices(services =>
{
    var options = new ExternalSorterSettings();

    var encoding = new UTF8Encoding(false);

    // Configure logging
    services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        
#if DEBUG
        logging.SetMinimumLevel(LogLevel.Debug);
#else
        logging.SetMinimumLevel(LogLevel.Information);
#endif
    });

    // Register application services
    services.AddSingleton<ExternalSorterSettings>(options);
    services.AddSingleton<Encoding>(encoding); 
    services.AddSingleton<IFileSystem, LocalFileSystem>();
    services.AddSingleton<Sorter>();
    services.AddSingleton<IMergeStrategy, PolyPhaseStrategyFiles>();
    services.AddSingleton<MemoryFileSorter>();
    services.AddSingleton<FileSplitter>();
    services.AddSingleton<FileGenerator>();
});

// Add commands with DI support
rootCommand.AddCommand<GenerateCommand>(serviceProvider);
rootCommand.AddCommand<SortCommand>(serviceProvider);

var stopwatch = Stopwatch.StartNew();

try
{
    return await rootCommand.Parse(args).InvokeAsync();
}
catch (Exception e)
{
    Console.WriteLine();
    Console.WriteLine("Failed due to the error: {0}.", e.Message);
    Console.WriteLine();
    Console.WriteLine(e);
    return 1;
}
finally
{
    stopwatch.Stop();
    Console.WriteLine($"Executing time is {stopwatch.ElapsedMilliseconds / 1000}");
}
