using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

namespace AirQuality.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private MenuItemViewModel _selectedMenuItem;
    private object _selectedContent;

    public MainWindowViewModel()
    {
        MenuItems = new ObservableCollection<MenuItemViewModel>
        {
            new MenuItemViewModel { Name = "Item 1", Content = new TextBlock { Text = "Content for Item 1" } },
            new MenuItemViewModel { Name = "Item 2", Content = new TextBlock { Text = "Content for Item 2" } },
            new MenuItemViewModel { Name = "Item 3", Content = new TextBlock { Text = "Content for Item 3" } }
        };
    }

    public ObservableCollection<MenuItemViewModel> MenuItems { get; }

    public MenuItemViewModel SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMenuItem, value);
            SelectedContent = value?.Content;
        }
    }

    public object SelectedContent
    {
        get => _selectedContent;
        private set => this.RaiseAndSetIfChanged(ref _selectedContent, value);
    }
}