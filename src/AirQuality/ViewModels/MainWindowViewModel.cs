using System;
using System.Windows.Input;
using ReactiveUI;

namespace AirQuality.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        TestCommand = ReactiveCommand.Create(() =>
        {
            // Code here will be executed when the button is clicked.
        });
    }

    public ICommand TestCommand { get; }

    public string Greeting => "Welcome to Avalonia!";
}