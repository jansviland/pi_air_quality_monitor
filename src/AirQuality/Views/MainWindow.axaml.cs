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