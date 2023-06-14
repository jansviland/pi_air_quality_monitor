using System;
using System.Collections.ObjectModel;
using AirQuality.Models;
using ReactiveUI;

namespace AirQuality.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private MenuItemSelectedModel _selectedStationStationMenuItem;
    private MenuItemAggregateModel _selectedAggregateMenuItem;
    private MenuItemViewOptionsModel _selectedViewOptionMenuItem;

    public ObservableCollection<MenuItemSelectedModel> StationsMenuItems { get; }
    public ObservableCollection<MenuItemAggregateModel> AggregateMenuItems { get; }
    public ObservableCollection<MenuItemViewOptionsModel> ViewOptions { get; }

    public MainWindowViewModel()
    {
        StationsMenuItems = new ObservableCollection<MenuItemSelectedModel>
        {
            new() { Name = "raspberry-pi-jan" },
            new() { Name = "Målestasjon 2" },
        };

        SelectedStationMenuItem = StationsMenuItems[0];

        ViewOptions = new ObservableCollection<MenuItemViewOptionsModel>
        {
            new() { Name = "Static View", GraphViewOption = GraphViewOptionsEnum.StaticView },
            new() { Name = "Animated View", GraphViewOption = GraphViewOptionsEnum.AnimatedView },
            new() { Name = "Live View", GraphViewOption = GraphViewOptionsEnum.LiveView }
        };

        SelectedViewOptionMenuItem = ViewOptions[0];

        AggregateMenuItems = new ObservableCollection<MenuItemAggregateModel>
        {
            // group by minute, hour, day, week, month
            new() { Name = "Minute", Window = null, MeanType = MeanType.Minute },
            new() { Name = "Hour", Window = null, MeanType = MeanType.Hour },
            new() { Name = "Day", Window = null, MeanType = MeanType.Day },
            new() { Name = "Week", Window = null, MeanType = MeanType.Week },
            new() { Name = "Month", Window = null, MeanType = MeanType.Month },

            // calculate simple moving average
            new() { Name = "10 min (SMA)", Window = TimeSpan.FromMinutes(10), MeanType = MeanType.SimpleMovingAverage },
            new() { Name = "30 min (SMA)", Window = TimeSpan.FromMinutes(30), MeanType = MeanType.SimpleMovingAverage },
            new() { Name = "1 hour (SMA)", Window = TimeSpan.FromHours(1), MeanType = MeanType.SimpleMovingAverage },
            new() { Name = "3 hours (SMA)", Window = TimeSpan.FromHours(3), MeanType = MeanType.SimpleMovingAverage },
            new() { Name = "6 hours (SMA)", Window = TimeSpan.FromHours(6), MeanType = MeanType.SimpleMovingAverage },
            new() { Name = "12 hours (SMA)", Window = TimeSpan.FromHours(12), MeanType = MeanType.SimpleMovingAverage },
            new() { Name = "24 hours (SMA)", Window = TimeSpan.FromHours(24), MeanType = MeanType.SimpleMovingAverage },
        };

        SelectedAggregateMenuItem = AggregateMenuItems[0];
    }

    public MenuItemViewOptionsModel SelectedViewOptionMenuItem
    {
        get => _selectedViewOptionMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedViewOptionMenuItem, value);
    }

    public MenuItemSelectedModel SelectedStationMenuItem
    {
        get => _selectedStationStationMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedStationStationMenuItem, value);
    }

    public MenuItemAggregateModel SelectedAggregateMenuItem
    {
        get => _selectedAggregateMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedAggregateMenuItem, value);
    }
}