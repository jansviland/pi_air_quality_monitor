using System;

namespace AirQuality.ViewModels;

public class MenuItemAggregateModel : ViewModelBase
{
    public string Name { get; set; } = null!;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}