using AirQuality.UserControls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AirQuality.Views;

public partial class SettingsWindow : Window
{
    private ListBox _optionsList;
    private ContentControl _mainView;

    public SettingsWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _optionsList = this.FindControl<ListBox>("OptionsList");
        _mainView = this.FindControl<ContentControl>("MainView");
        _optionsList.SelectionChanged += OptionsList_SelectionChanged;
        UpdateMainView();
    }

    private void OptionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateMainView();
    }

    private void UpdateMainView()
    {
        switch (_optionsList.SelectedIndex)
        {
            case 0:
                _mainView.Content = new SettingsGeneralUserControl();
                break;
            case 1:
                _mainView.Content = new SettingsDatabaseUserControl();
                break;
            default:
                _mainView.Content = null;
                break;
        }
    }
}