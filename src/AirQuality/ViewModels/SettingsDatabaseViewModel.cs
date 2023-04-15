using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AirQuality.ViewModels;

public class SettingsDatabaseViewModel : INotifyPropertyChanged
{
    // Implement INotifyPropertyChanged interface
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Add your properties here
    private string _databaseConnectionString;

    public string DatabaseConnectionString
    {
        get => _databaseConnectionString;
        set
        {
            _databaseConnectionString = value;
            OnPropertyChanged();
        }
    }

    // SaveDatabaseSettingsCommand
    private ICommand _saveDatabaseSettingsCommand;

    public ICommand SaveDatabaseSettingsCommand
    {
        get
        {
            if (_saveDatabaseSettingsCommand == null)
            {
                // _saveDatabaseSettingsCommand = new RelayCommand(param => SaveDatabaseSettings());
            }

            return _saveDatabaseSettingsCommand;
        }
        set => _saveDatabaseSettingsCommand = value;
    }

    // public void SaveDatabaseSettingsCommand { get; }

    // Add other properties (DatabaseName, DatabaseUser, etc.) and the SaveDatabaseSettingsCommand in a similar manner
}