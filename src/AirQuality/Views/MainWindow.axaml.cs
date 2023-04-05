using System;
using AirQuality.ViewModels;
using Avalonia;
using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;

namespace AirQuality.Views;

public partial class MainWindow : Window
{
    private readonly Random _rand = new Random();
    private const int PointCount = 100;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainWindowViewModel();

#if DEBUG
        this.AttachDevTools();
#endif

        var listBox = this.FindControl<ListBox>("MenuListBox");
        listBox.SelectionChanged += MenuListBox_SelectionChanged;
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