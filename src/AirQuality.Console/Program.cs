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

        var svc = ActivatorUtilities.CreateInstance<Service>(host.Services);

        if (args.Length == 0)
        {
            System.Console.WriteLine("dotnet run <path to file>");
            return;
        }

        if (File.Exists(args[0]) == false)
        {
            System.Console.WriteLine("File does not exist");
            return;
        }

        var input = File.ReadAllLines(args[0]);

        svc.Run(input);
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