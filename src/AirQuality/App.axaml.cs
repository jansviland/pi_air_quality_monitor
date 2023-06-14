using System;
using System.IO;
using AirQuality.DataLayer;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AirQuality.ViewModels;
using AirQuality.Views;
using Avalonia.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AirQuality;

public class App : Application
{
    public Window MainWindow { get; set; }

    private static IHost Host { get; set; } = null!;

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IDatabase, Database>();
        services.AddTransient<IBlobStorage, BlobStorage>();
        services.AddTransient<ILocalJsonStorage, LocalJsonStorage>();
        services.AddSingleton<MainWindow>();
    }

    public override void Initialize()
    {
        var builder = new ConfigurationBuilder();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(BuildConfiguration(builder))
            .Enrich.FromLogContext()
            .CreateLogger();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) => { ConfigureServices(services); })
            .UseSerilog()
            .Build();

        Log.Logger.Information("Starting application...");

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Host.Services.GetRequiredService<MainWindow>();
            desktop.MainWindow.DataContext = new MainWindowViewModel();
            this.MainWindow = desktop.MainWindow; // Store the reference to the main window.
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IConfiguration BuildConfiguration(IConfigurationBuilder builder)
    {
        // BUG: on mac it looks for appsettings.json in the user folder instead of where the app is built

        builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        return configuration;
    }
}