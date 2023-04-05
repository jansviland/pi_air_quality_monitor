using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using ScottPlot;
using ScottPlot.Avalonia;

namespace AirQuality.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private MenuItemViewModel _selectedMenuItem;
    private object _selectedContent;

    public Plot Plot;

    public MainWindowViewModel()
    {
        MenuItems = new ObservableCollection<MenuItemViewModel>
        {
            new MenuItemViewModel { Name = "raspberry-pi-jan", Content = null },
            new MenuItemViewModel { Name = "Item 2", Content = null },
            new MenuItemViewModel { Name = "Item 3", Content = null }
        };

        SelectedMenuItem = MenuItems[0];
    }

    public ObservableCollection<MenuItemViewModel> MenuItems { get; }

    public MenuItemViewModel SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMenuItem, value);

            SelectedContent = value?.Content;

            if (value?.Name == "raspberry-pi-jan")
            {
                Plot = CreateScottPlot();
                // SelectedContent = new AvaPlot(_plot);
            }
        }
    }

    public object SelectedContent
    {
        get => _selectedContent;
        private set => this.RaiseAndSetIfChanged(ref _selectedContent, value);
    }

    private Plot CreateScottPlot()
    {
        var plt = new Plot(600, 400);

        // create data sample data
        double[] ys = DataGen.RandomWalk(100);

        TimeSpan ts = TimeSpan.FromSeconds(1); // time between data points
        double sampleRate = (double)TimeSpan.TicksPerDay / ts.Ticks;
        var signalPlot = plt.AddSignal(ys, sampleRate);

        // Then tell the axis to display tick labels using a time format
        plt.XAxis.DateTimeFormat(true);

        // Set start date
        signalPlot.OffsetX = new DateTime(1985, 10, 1).ToOADate();

        // Create and return the AvaPlotViewer
        return plt;
    }
}