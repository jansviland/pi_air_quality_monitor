using System.ComponentModel;
using AirQuality.ViewModels;
using Avalonia;
using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;

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

        double[] dataX = new double[] { 1, 2, 3, 4, 5 };
        double[] dataY = new double[] { 1, 4, 9, 16, 25 };
        AvaPlot avaPlot1 = this.Find<AvaPlot>("AvaPlot1");
        avaPlot1.Plot.AddScatter(dataX, dataY);
        avaPlot1.Refresh();
    }
}