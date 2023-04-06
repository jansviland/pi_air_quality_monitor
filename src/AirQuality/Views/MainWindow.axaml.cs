using System;
using System.Collections.Generic;
using AirQuality.Models;
using AirQuality.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScottPlot;
using ScottPlot.Avalonia;

namespace AirQuality.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;

    private readonly Random _rand = new Random();
    private const int PointCount = 100;

    private readonly string? _connectionString;
    private readonly List<Measurement> _measurements = new List<Measurement>();

    public MainWindow()
    {
    }

    public MainWindow(ILogger<MainWindow> logger, IConfiguration configuration)
    {
        _logger = logger;
        InitializeComponent();

        DataContext = new MainWindowViewModel();

#if DEBUG
        this.AttachDevTools();
#endif

        var listBox = this.FindControl<ListBox>("MenuListBox");
        listBox.SelectionChanged += MenuListBox_SelectionChanged;

        // get connection string from appsettings.json
        _connectionString = configuration.GetConnectionString("DefaultConnection");

        _measurements = GetMeasurements();

        // TODO: connect to DB
        // TODO: show error if connection fails
        // TODO: get data from DB
        // TODO: show loading indicator
    }

    /// <summary>
    /// Read in all rows from the Dogs1 table and store them in a List.
    /// </summary>
    private List<Measurement> GetMeasurements()
    {
        List<Measurement> measurements = new List<Measurement>();
        using (SqlConnection con = new SqlConnection(_connectionString))
        {
            con.Open();

            using (SqlCommand command = new SqlCommand("SELECT * FROM measurements", con))
            {
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    measurements.Add(new Measurement()
                    {
                        Pm2 = reader.GetDouble(0),
                        Pm10 = reader.GetDouble(1),
                        // EventProcessedUtcTime = reader.GetDateTime(2),
                        // PartitionId = reader.GetInt64(3),
                        EventEnqueuedUtcTime = reader.GetDateTime(4),
                        // IoTHub = reader.GetString(5),

                        // unixtime can be null in the database, added it later on, so in some earlier rows it will be null
                        UnixTime = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                        // ClientId = reader.GetString(7)
                    });
                }
            }
        }

        foreach (Measurement measurement in measurements)
        {
            Console.WriteLine(measurement);
        }

        return measurements;
    }

    private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO: get data from blob storage or database

        // TODO: can use Axis Span to show the different levels of air quality
        // 0-50 is good, 51-100 is moderate, 101-150 is unhealthy for sensitive groups, 151-200 is unhealthy, 201-300 is very unhealthy, 301-500 is hazardous

        // TODO: allow for changing style, to dark mode or light mode, use Blue1 style

        // TODO: use ToolTip to special events, like when the air quality is bad

        // TODO: should say that NaN values Break the line, should not fill the gap with a line when values are missing

        // TODO: use brackets to show the range of the data, like [0, 100], for example if something happened in this period of time


        if (e.AddedItems.Count > 0 && e.AddedItems[0] is MenuItemViewModel menuItem)
        {
            _logger.LogInformation($"Selected menu item: {menuItem.Name}");

            var avaPlot = this.FindControl<AvaPlot>("AvaPlot1");
            avaPlot.Plot.Clear();
            avaPlot.Plot.Title(menuItem.Name);

            // create data sample data
            double[] ys = DataGen.RandomWalk(_rand, PointCount);
            TimeSpan ts = TimeSpan.FromSeconds(1); // time between data points
            double sampleRate = (double)TimeSpan.TicksPerDay / ts.Ticks;
            var signalPlot = avaPlot.Plot.AddSignal(ys, sampleRate);

            // Then tell the axis to display tick labels using a time format
            avaPlot.Plot.XAxis.DateTimeFormat(true);

            // Set start date
            signalPlot.OffsetX = new DateTime(1985, 10, 1).ToOADate();

            avaPlot.Render();
        }
    }
}