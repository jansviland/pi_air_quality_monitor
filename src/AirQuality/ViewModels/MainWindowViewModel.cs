using System.Collections.ObjectModel;
using ReactiveUI;

namespace AirQuality.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private MenuItemViewModel _selectedMenuItem;

    public ObservableCollection<MenuItemViewModel> MenuItems { get; }

    public MainWindowViewModel()
    {
        MenuItems = new ObservableCollection<MenuItemViewModel>
        {
            new() { Name = "raspberry-pi-jan" },
            new() { Name = "Målestasjon 2" },
            new() { Name = "Målestasjon 3" }
        };

        SelectedMenuItem = MenuItems[0];
    }

    public MenuItemViewModel SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedMenuItem, value);
    }
}