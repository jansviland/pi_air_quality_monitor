using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AirQuality.UserControl;

public partial class SettingsGeneralUserControl : Avalonia.Controls.UserControl
{
    public SettingsGeneralUserControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}