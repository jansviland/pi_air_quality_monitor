using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirQuality.Common.Extensions;
using AirQuality.Common.Helpers;
using AirQuality.Common.Models;
using AirQuality.DataLayer;
using AirQuality.Models;
using AirQuality.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DynamicData;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ScottPlot;
using ScottPlot.Avalonia;

namespace AirQuality.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly IDatabase _database;
    private readonly IBlobStorage _blobStorage;
    private readonly ILocalJsonStorage _localJsonStorage;

    private readonly List<Measurement> _minuteMeasurements = new();
    private List<Measurement> _measurements = new();

    private DispatcherTimer _timer;

    // set the default aggregation window to 1 minute
    private TimeSpan _aggregateWindow = TimeSpan.FromMinutes(1);
    private MeanType _meanType = MeanType.Minute;

    // for enabling and disabling the animation of the graph
    private bool _animateGraph;
    private CancellationTokenSource _cts;

    public MainWindow()
    {
    }

    public MainWindow(ILogger<MainWindow> logger, IDatabase database, IBlobStorage blobStorage, ILocalJsonStorage localJsonStorage)
    {
        _logger = logger;
        _database = database;
        _blobStorage = blobStorage;
        _localJsonStorage = localJsonStorage;

        InitializeComponent();

        DataContext = new MainWindowViewModel();

#if DEBUG
        this.AttachDevTools();
#endif

        // timer for updating the graf every minute
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromMinutes(1);
        _timer.Tick += GetLatestMeasurmentsTimerTick;

        var stationlistBox = this.FindControl<ListBox>("StationsMenuListBox");
        stationlistBox.SelectionChanged += StationsMenuListBox_SelectionChanged;

        var viewOptionsListBox = this.FindControl<ListBox>("ViewOptionsMenuListBox");
        viewOptionsListBox.SelectionChanged += ViewOptionsMenuListBox_SelectionChanged;

        var aggregateListBox = this.FindControl<ListBox>("AggregationMenuListBox");
        aggregateListBox.SelectionChanged += AggregateListBoxOnSelectionChanged;

        // TODO: get data locally from json files, to update, get data from Azure and store locally once. Then use the local data again.
        // _measurements = _database.GetMeasurements(60); // last 60 measurements (1 hour)

        // TODO: this can take a while, so we should show a loading indicator, and show an error if it fails
        // TODO: trigger via settings page, don't do it automatically on startup
        // _blobStorage.UpdateLocalFiles();

        var datesWithMeasurements = _localJsonStorage.GetDatesWithMeasurments();

        // This will test DB connection, if it fails show error message
        try
        {
            datesWithMeasurements.Add(_database.GetDatesWithMeasurments());
            datesWithMeasurements.Sort();
        }
        catch (SqlException e)
        {
            MessageTextBlock.Text = e.Message;

            // show popup error message
            // var errorWindow = new ErrorMessageWindow("Your error message");
            // errorWindow.ShowDialog(((App)Application.Current).MainWindow);

            return;
        }

        // remove duplicates
        var uniqueDatesWithMeasurements = datesWithMeasurements.Distinct().ToList();

        // black out dates before the first date with measurements
        AvailableDatesCalendar.IsTodayHighlighted = false;
        AvailableDatesCalendar.BlackoutDates.AddRange(new[] { new CalendarDateRange(DateTime.MinValue, uniqueDatesWithMeasurements[0] + TimeSpan.FromDays(-1)) });
        AvailableDatesCalendar.BlackoutDates.AddRange(new[] { new CalendarDateRange(uniqueDatesWithMeasurements[^1] + TimeSpan.FromDays(1), DateTime.MaxValue) });

        // go through from date to the end date, and check if there are measurements for that date. If there are no measurements, then we should not be able to select that date
        foreach (var dateTime in EachDay(uniqueDatesWithMeasurements[0], uniqueDatesWithMeasurements[^1]))
        {
            // TODO: also check if there are measurements in the SQL Database, then this should not be blacked out
            if (!_localJsonStorage.HasMeasurementsForDate(dateTime) && !_database.HasMeasurementsForDate(dateTime))
            {
                AvailableDatesCalendar.BlackoutDates.Add(new CalendarDateRange(dateTime, dateTime));
            }
        }

        // set selected date to the last date with measurements
        AvailableDatesCalendar.SelectedDate = uniqueDatesWithMeasurements[^1];

        // TODO: show error if connection fails
        // TODO: show loading indicator
    }

    private async void Update()
    {
        _cts?.Cancel();

        if (_animateGraph)
        {
            _cts = new CancellationTokenSource();


            try
            {
                // order the measurements by date
                _measurements = _measurements.OrderBy(m => m.UtcTime).ToList();

                await UpdateGraphAnimatedAsync(_measurements, _cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                // This happens when the user clicks on a new graph before the previous one has finished animating, not a problem
                _logger.LogInformation(ex, "UpdateGraphAnimatedAsync was cancelled");
            }
            // catch (KeyNotFoundException ex)
            // {
            //     _logger.LogError(ex, "KeyNotFoundException");
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, "Exception");
            // }
        }
        else
        {
            UpdateGraph(_measurements);
        }
    }

    private async void AggregateListBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count <= 0 || e.AddedItems[0] is not MenuItemAggregateModel menuItem) return;

        _logger.LogInformation($"AggregateListBoxOnSelectionChanged: {menuItem.Name}");

        _meanType = menuItem.MeanType;

        if (menuItem.MeanType == MeanType.SimpleMovingAverage)
        {
            _aggregateWindow = menuItem.Window!.Value;
        }

        // TODO: check if the measurements are already aggregated, if they are, then we should not aggregate them again
        // TODO: after aggregation, we should save the aggregated measurements to a json file ex. "2023-06-10-minute-interval-to-2-hour-moving-avg.json", so we don't have to aggregate them again

        switch (_meanType)
        {
            case MeanType.Minute:
                _measurements = _minuteMeasurements;
                break;
            case MeanType.Hour:
                _measurements = AggregateHelper.GetHourAggregate(_minuteMeasurements).ToList();
                break;
            case MeanType.Day:
                _measurements = AggregateHelper.GetDayAggregate(_minuteMeasurements).ToList();
                break;
            case MeanType.Week:
                break;
            case MeanType.Month:
                break;
            case MeanType.SimpleMovingAverage:
            {
                var interval = TimeSpan.FromMinutes(1);
                _measurements = AggregateHelper.CalculateSimpleMovingAverage(_minuteMeasurements, _aggregateWindow, interval).ToList();
                break;
            }
        }

        Update();
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

    private void ViewOptionsMenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is MenuItemViewOptionsModel menuItem)
        {
            _logger.LogInformation($"ViewOptionsMenuListBox_SelectionChanged: {menuItem.Name}");

            switch (menuItem.GraphViewOption)
            {
                case GraphViewOptionsEnum.StaticView:
                    _timer.Stop();
                    _animateGraph = false;
                    Update();
                    break;
                case GraphViewOptionsEnum.AnimatedView:
                    _timer.Stop();
                    _animateGraph = true;
                    Update();
                    break;
                case GraphViewOptionsEnum.LiveView:
                    _timer.Stop();
                    _animateGraph = false;

                    AvailableDatesCalendar.SelectedDate = DateTime.Now;

                    // trigger the timer tick event, so that we can show the graph for the current date
                    GetLatestMeasurmentsTimerTick(null, null);

                    // every minute, get two hours of data from the SQL database, and then show the graph for that data
                    _timer.Start();

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void StationsMenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO: get data from blob storage or database

        // TODO: can use Axis Span to show the different levels of air quality
        // 0-50 is good, 51-100 is moderate, 101-150 is unhealthy for sensitive groups, 151-200 is unhealthy, 201-300 is very unhealthy, 301-500 is hazardous

        // TODO: allow for changing style, to dark mode or light mode, use Blue1 style

        // TODO: use ToolTip to special events, like when the air quality is bad

        // TODO: should say that NaN values Break the line, should not fill the gap with a line when values are missing

        // TODO: use brackets to show the range of the data, like [0, 100], for example if something happened in this period of time

        // if (_measurements.Count == 0)
        // {
        //     return;
        // }
        //
        // if (e.AddedItems.Count > 0 && e.AddedItems[0] is MenuItemViewModel menuItem)
        // {
        //     UpdateGraph(_measurements);
        // }
    }

    private void OnDisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
    {
        // _timer.Stop();
        // Handle the DisplayDateChanged event here
    }

    private List<Measurement> GetMeasurementsForDate(DateTime date)
    {
        var measurements = _localJsonStorage.GetMeasurementsForDate(date);

        // can not find data locally, so we need to get it from the SQL database
        if (measurements == null)
        {
            measurements = _database.GetMeasurementsForDate(date);
            MessageTextBlock.Text = $"Found {measurements.Count} measurements for {date.ToShortDateString()} using SQL database.";

            // save them as json files locally
            _localJsonStorage.SaveMeasurementsForDate(date, measurements);
        }
        else
        {
            MessageTextBlock.Text = $"Found {measurements.Count} measurements for {date.ToShortDateString()} using local storage.";
        }

        return measurements;
    }

    private async void OnSelectedDatesChanged(object sender, SelectionChangedEventArgs e)
    {
        _timer.Stop();

        var selectedDates = AvailableDatesCalendar.SelectedDates.ToList();

        _measurements.Clear();
        _minuteMeasurements.Clear();

        foreach (var date in selectedDates)
        {
            var measurementsForSpesificDate = GetMeasurementsForDate(date);

            _measurements.AddRange(measurementsForSpesificDate);
            _minuteMeasurements.Add(measurementsForSpesificDate);
        }

        switch (_meanType)
        {
            case MeanType.Minute:
                _measurements = _minuteMeasurements;
                break;
            case MeanType.Hour:
                _measurements = AggregateHelper.GetHourAggregate(_minuteMeasurements).ToList();
                break;
            case MeanType.Day:
                _measurements = AggregateHelper.GetDayAggregate(_minuteMeasurements).ToList();
                break;
            case MeanType.Week:
                break;
            case MeanType.Month:
                break;
            case MeanType.SimpleMovingAverage:
            {
                var interval = TimeSpan.FromMinutes(1);
                _measurements = AggregateHelper.CalculateSimpleMovingAverage(_minuteMeasurements, _aggregateWindow, interval).ToList();
                break;
            }
        }

        Update();
    }

    private void UpdateGraph(List<Measurement> measurements)
    {
        // https://www.scottplot.net/cookbook/5.0/

        // TODO: restrict y axis to -5 to 25

        var avaPlot = this.FindControl<AvaPlot>("AvaPlot1");
        avaPlot.Plot.Clear();

        if (measurements.Count == 0)
        {
            avaPlot.InvalidateVisual();
            return;
        }

        var startDate = measurements[0].UtcTime.ToNorwegianDateTimeString();
        var endDate = measurements[^1].UtcTime.ToNorwegianDateTimeString();
        var clientName = measurements[0].ClientId;

        var title = $"{clientName}: {startDate} - {endDate} ({measurements.Count} measurements)";

        avaPlot.Plot.Title.Label.Text = title;
        avaPlot.Plot.YAxis.Label.Text = "µg/m³";

        // convert the the measurements to arrays
        var xs = new double[measurements.Count];
        var pm2 = new double[measurements.Count];
        var pm10 = new double[measurements.Count];

        for (var i = 0; i < measurements.Count; i++)
        {
            xs[i] = measurements[i].UtcTime.ToOADate();
            pm2[i] = measurements[i].Pm2;
            pm10[i] = measurements[i].Pm10;
        }

        var pm2Scatter = avaPlot.Plot.Add.Scatter(xs, pm2);
        pm2Scatter.Label = "PM2.5";

        var pm10Scatter = avaPlot.Plot.Add.Scatter(xs, pm10);
        pm10Scatter.Label = "PM10";

        // tell the axis to display tick labels using a time format
        avaPlot.Plot.Axes.DateTimeTicks(Edge.Bottom);

        avaPlot.InvalidateVisual();
    }

    private async Task UpdateGraphAnimatedAsync(List<Measurement> measurements, CancellationToken cancellationToken = default)
    {
        // https://www.scottplot.net/cookbook/5.0/

        // TODO: restrict y axis to -5 to 25

        var avaPlot = this.FindControl<AvaPlot>("AvaPlot1");
        avaPlot.Plot.Clear();

        if (measurements.Count == 0)
        {
            avaPlot.InvalidateVisual();
            return;
        }

        var startDate = measurements[0].UtcTime.ToLongTimeString();
        var endDate = measurements[^1].UtcTime.ToLongTimeString();
        var clientName = measurements[0].ClientId;

        var title = $"{clientName}: {startDate} - {endDate} ({measurements.Count} measurements)";
        avaPlot.Plot.Title.Label.Text = title;
        // avaPlot.Plot.XAxis.Label.Text = "Horizonal Axis";
        // avaPlot.Plot.YAxis.Label.Text = "Vertical Axis";

        // convert the the measurements to arrays
        var xs = new double[measurements.Count];
        var pm2 = new double[measurements.Count];
        var pm10 = new double[measurements.Count];

        // Clear the data arrays
        Array.Clear(xs, 0, xs.Length);
        Array.Clear(pm2, 0, pm2.Length);
        Array.Clear(pm10, 0, pm10.Length);

        // Add values one at a time with a delay
        for (var i = 0; i < measurements.Count && !cancellationToken.IsCancellationRequested; i++)
        {
            xs[i] = measurements[i].UtcTime.ToOADate();
            pm2[i] = measurements[i].Pm2;
            pm10[i] = measurements[i].Pm10;

            avaPlot.Plot.Clear();

            // show max two days of data (24 hours / 1440 minutes)
            var skip = Math.Max(0, i - 1440);
            var take = Math.Min(i + 1, 1440);

            var pm2Scatter = avaPlot.Plot.Add.Scatter(xs.Skip(skip).Take(take).ToArray(), pm2.Skip(skip).Take(take).ToArray());
            pm2Scatter.Label = "PM2.5";

            var pm10Scatter = avaPlot.Plot.Add.Scatter(xs.Skip(skip).Take(take).ToArray(), pm10.Skip(skip).Take(take).ToArray());
            pm10Scatter.Label = "PM10";

            // Update the title with the latest endDate and count
            var firstStartDate = measurements[skip].UtcTime.ToNorwegianDateTimeString();
            var latestEndDate = measurements[i].UtcTime.ToNorwegianDateTimeString();
            var latestCount = take;
            var updatedTitle = $"{clientName}: {firstStartDate} - {latestEndDate} ({latestCount} measurements)";
            avaPlot.Plot.Title.Label.Text = updatedTitle;

            avaPlot.Plot.Axes.DateTimeTicks(Edge.Bottom);

            // Trigger a re-render
            avaPlot.InvalidateVisual();

            // Add a delay between adding each data point
            await Task.Delay(10, cancellationToken); // 10 ms delay, adjust as needed
        }
    }

    private void GetLatestMeasurmentsTimerTick(object? sender, EventArgs e)
    {
        MessageTextBlock.Text = $"Status: Updated graph: {DateTime.Now}";

        var measurements = _database.GetLatestMeasurements(240);

        UpdateGraph(measurements);
    }

    private static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
    {
        for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
        {
            yield return day;
        }
    }

    private void MenuItem_Settings_OnClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Show(); // Use ShowDialog() if you want to open the settings window as a modal dialog
    }
}