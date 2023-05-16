using System.Collections.ObjectModel;
using AirQuality.Models;
using ReactiveUI;

namespace AirQuality.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private MenuItemSelectedModel _selectedStationStationMenuItem;
    private MenuItemSelectedModel _selectedAggregateMenuItem;
    private MenuItemViewOptionsModel _selectedViewOptionMenuItem;

    public ObservableCollection<MenuItemSelectedModel> StationsMenuItems { get; }
    public ObservableCollection<MenuItemSelectedModel> AggregateMenuItems { get; }
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

        AggregateMenuItems = new ObservableCollection<MenuItemSelectedModel>
        {
            new() { Name = "1 min" },
            new() { Name = "5 min" },
            new() { Name = "10 min" },
            new() { Name = "30 min" },
            new() { Name = "1 hour" },
            new() { Name = "6 hours" },
            new() { Name = "12 hours" },
            new() { Name = "24 hours" },
            // new() { Name = "1 week" },
            // new() { Name = "1 month" },
            // new() { Name = "1 year" }
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

    public MenuItemSelectedModel SelectedAggregateMenuItem
    {
        get => _selectedAggregateMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedAggregateMenuItem, value);
    }
}