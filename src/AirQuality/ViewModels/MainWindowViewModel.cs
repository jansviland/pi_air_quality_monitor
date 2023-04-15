using System.Collections.ObjectModel;
using AirQuality.Models;
using ReactiveUI;

namespace AirQuality.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private MenuItemStationSelectedModel _selectedStationMenuItem;
    private MenuItemViewOptionsModel _selectedViewOptionMenuItem;

    public ObservableCollection<MenuItemStationSelectedModel> MenuItems { get; }
    public ObservableCollection<MenuItemViewOptionsModel> ViewOptions { get; }

    public MainWindowViewModel()
    {
        MenuItems = new ObservableCollection<MenuItemStationSelectedModel>
        {
            new() { Name = "raspberry-pi-jan" },
            new() { Name = "Målestasjon 2" },
            new() { Name = "Målestasjon 3" }
        };

        SelectedStationMenuItem = MenuItems[0];

        ViewOptions = new ObservableCollection<MenuItemViewOptionsModel>
        {
            new() { Name = "Static View", GraphViewOption = GraphViewOptionsEnum.StaticView },
            new() { Name = "Animated View", GraphViewOption = GraphViewOptionsEnum.AnimatedView },
            new() { Name = "Live View", GraphViewOption = GraphViewOptionsEnum.LiveView }
        };

        SelectedViewOptionMenuItem = ViewOptions[0];
    }

    public MenuItemViewOptionsModel SelectedViewOptionMenuItem
    {
        get => _selectedViewOptionMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedViewOptionMenuItem, value);
    }

    public MenuItemStationSelectedModel SelectedStationMenuItem
    {
        get => _selectedStationMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedStationMenuItem, value);
    }
}