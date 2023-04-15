using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AirQuality.UserControl;

public partial class SettingsDatabaseUserControl : Avalonia.Controls.UserControl
{
    public SettingsDatabaseUserControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}