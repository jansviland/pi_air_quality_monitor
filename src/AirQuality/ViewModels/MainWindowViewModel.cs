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
            new() { Name = "1 min", Window = TimeSpan.FromMinutes(1)},
            new() { Name = "5 min", Window = TimeSpan.FromMinutes(5) },
            new() { Name = "10 min", Window = TimeSpan.FromMinutes(10)},
            new() { Name = "30 min", Window = TimeSpan.FromMinutes(30)},
            new() { Name = "1 hour", Window = TimeSpan.FromHours(1)},
            new() { Name = "6 hours", Window = TimeSpan.FromHours(6)},
            new() { Name = "12 hours", Window = TimeSpan.FromHours(12)},
            new() { Name = "24 hours", Window = TimeSpan.FromHours(24)},
            new() { Name = "1 week", Window = TimeSpan.FromDays(7)},
            new() { Name = "1 month", Window = TimeSpan.FromDays(30)},
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