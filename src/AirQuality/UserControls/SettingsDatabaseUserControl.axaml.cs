using AirQuality.ViewModels;
using Avalonia.Markup.Xaml;

namespace AirQuality.UserControls;

public partial class SettingsDatabaseUserControl : Avalonia.Controls.UserControl
{
    public SettingsDatabaseUserControl()
    {
        InitializeComponent();
        DataContext = new SettingsDatabaseViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}