using AirQuality.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace AirQuality.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainWindowViewModel();

#if DEBUG
        this.AttachDevTools();
#endif
    }
}