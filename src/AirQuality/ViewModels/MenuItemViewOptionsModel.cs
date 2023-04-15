using AirQuality.Models;

namespace AirQuality.ViewModels;

public class MenuItemViewOptionsModel : ViewModelBase
{
    public string Name { get; set; } = null!;
    public GraphViewOptionsEnum GraphViewOption { get; set; }
}