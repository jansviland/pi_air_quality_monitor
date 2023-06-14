using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AirQuality.Views;

public partial class ErrorMessageWindow : Window
{
    public ErrorMessageWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public ErrorMessageWindow(string message) : this()
    {
        this.FindControl<TextBlock>("ErrorMessage").Text = message;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OKButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }
}