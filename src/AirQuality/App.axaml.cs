using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AirQuality.ViewModels;
using AirQuality.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AirQuality;

public class App : Application
{
    private static IHost Host { get; set; } = null!;

    public static void ConfigureServices(IServiceCollection services)
    {
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
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IConfiguration BuildConfiguration(IConfigurationBuilder builder)
    {
        builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        return configuration;
    }
}