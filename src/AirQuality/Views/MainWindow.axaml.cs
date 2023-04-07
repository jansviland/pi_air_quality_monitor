using System.Collections.Generic;
using AirQuality.Common.Models;
using AirQuality.DataLayer;
using AirQuality.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using ScottPlot.Avalonia;

namespace AirQuality.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly IDatabase _database;
    private readonly IBlobStorage _blobStorage;

    private readonly List<Measurement> _measurements = new();

    public MainWindow()
    {
    }

    public MainWindow(ILogger<MainWindow> logger, IDatabase database, IBlobStorage blobStorage)
    {
        _logger = logger;
        _database = database;
        _blobStorage = blobStorage;

        InitializeComponent();

        DataContext = new MainWindowViewModel();

#if DEBUG
        this.AttachDevTools();
#endif

        var listBox = this.FindControl<ListBox>("MenuListBox");
        listBox.SelectionChanged += MenuListBox_SelectionChanged;

        // TODO: get data locally from json files, to update, get data from Azure and store locally once. Then use the local data again.
        // _measurements = _database.GetMeasurements(60); // last 60 measurements (1 hour)

        // TODO: this can take a while, so we should show a loading indicator, and show an error if it fails
        _blobStorage.UpdateLocalFiles();

        // TODO: show error if connection fails
        // TODO: show loading indicator
    }

    // TODO: add a feature, where you can select a data point, and then you should be able to change this value in the database
    // for example, set that one measurement to invalid. Then invalid measurements should not be shown in the graph

    // TODO: add a feature, should be able to select day, week, month or other time range

    // TODO: make an sql query that count the number of measurements for each month, week, day etc. So we can see if it's possible to find
    // measurements for that time range. For example if it's around 1440 measurements at the 02.01.2021, then it's possible to show the graph for that day.
    // it has the full amount of 60 * 24 measurements for that day. Then we should be able to select that day.
    // if it's 0 measurements for that day, then we should not be able to select that day. It should be greyed out, or something like that

    // TODO: get x amount of measurements from the database at a time, and then add them to the graph, test how many measurements can be shown on the graph at the same time
    // TODO: make a "live data" feature, for when the sql stream is active, then you should be able to see the measurements in real time, minute by minute

    // TODO: for longer time frames, calculate the average value for each hour, and then show that on the graph
    // TODO: for even longer time frames, calculate the average value for each day, and then show that on the graph
    // TODO: store the values locally, so that you don't have to query the database every time you want to show the graph

    private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO: get data from blob storage or database

        // TODO: can use Axis Span to show the different levels of air quality
        // 0-50 is good, 51-100 is moderate, 101-150 is unhealthy for sensitive groups, 151-200 is unhealthy, 201-300 is very unhealthy, 301-500 is hazardous

        // TODO: allow for changing style, to dark mode or light mode, use Blue1 style

        // TODO: use ToolTip to special events, like when the air quality is bad

        // TODO: should say that NaN values Break the line, should not fill the gap with a line when values are missing

        // TODO: use brackets to show the range of the data, like [0, 100], for example if something happened in this period of time

        if (_measurements.Count == 0)
        {
            return;
        }

        if (e.AddedItems.Count > 0 && e.AddedItems[0] is MenuItemViewModel menuItem)
        {
            var avaPlot = this.FindControl<AvaPlot>("AvaPlot1");
            avaPlot.Plot.Clear();
            avaPlot.Plot.Title(menuItem.Name);

            // convert the the measurements to arrays
            var xs = new double[_measurements.Count];
            var pm2 = new double[_measurements.Count];
            var pm10 = new double[_measurements.Count];

            for (var i = 0; i < _measurements.Count; i++)
            {
                xs[i] = _measurements[i].EventEnqueuedUtcTime.ToOADate();
                pm2[i] = _measurements[i].Pm2;
                pm10[i] = _measurements[i].Pm10;
            }

            // add the measurements to the plot
            avaPlot.Plot.AddScatter(xs, pm2, label: "PM2.5");
            avaPlot.Plot.AddScatter(xs, pm10, label: "PM10");
            avaPlot.Plot.Legend();

            // tell the axis to display tick labels using a time format
            avaPlot.Plot.XAxis.DateTimeFormat(true);

            avaPlot.Render();
        }
    }
}