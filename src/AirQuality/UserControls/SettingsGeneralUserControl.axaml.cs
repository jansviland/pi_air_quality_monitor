using Avalonia.Markup.Xaml;

namespace AirQuality.UserControls;

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