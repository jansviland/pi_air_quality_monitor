using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AirQuality.Console;

internal static class Program
{
    private static void Main(string[] args)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var builder = new ConfigurationBuilder();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(BuildConfiguration(builder))
            .Enrich.FromLogContext()
            .CreateLogger();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(((_, collection) => { collection.AddTransient<IService, Service>(); }))
            .UseSerilog()
            .Build();

        // Log.Logger.Information("args: {AllArguments}", string.Join(", ", args));

        var svc = ActivatorUtilities.CreateInstance<Service>(host.Services);

        // arguments should be:
        // -f <path to file> -c <connection string>

        // if (args.Length == 0)
        // {
        //     System.Console.WriteLine("No arguments provided");
        //     System.Console.WriteLine("dotnet run -f <path to file> -c <connection string>");
        //
        //     return;
        // }
        // else if (args.Length != 4)
        // {
        //     System.Console.WriteLine("Invalid number of arguments provided");
        //     System.Console.WriteLine("dotnet run -f <path to file> -c <connection string>");
        //
        //     return;
        // }
        // else if (args[0] != "-f" || args[2] != "-c")
        // {
        //     System.Console.WriteLine("Invalid arguments provided");
        //     System.Console.WriteLine("dotnet run -f <path to file> -c <connection string>");
        //
        //     return;
        // }
        // else if (!File.Exists(args[1]))
        // {
        //     System.Console.WriteLine("File does not exist");
        //     System.Console.WriteLine("dotnet run -f <path to file> -c <connection string>");
        //
        //     return;
        // }
        // else if (string.IsNullOrWhiteSpace(args[3]))
        // {
        //     System.Console.WriteLine("Connection string is empty");
        //     System.Console.WriteLine("dotnet run -f <path to file> -c <connection string>");
        //
        //     return;
        // }

        // var input = File.ReadAllLines(args[1]);
        // var connectionString = args[3];

        var input = File.ReadAllLines("Assets/measurements.csv");
        string? connectionString = null;

        svc.Run(input, connectionString);
        Log.Logger.Information("Data bulk inserted successfully!");

        stopWatch.Stop();
        Log.Logger.Information("Elapsed time: {Elapsed} ms", stopWatch.ElapsedMilliseconds);
    }

    private static IConfiguration BuildConfiguration(IConfigurationBuilder builder)
    {
        builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        return configuration;
    }
}