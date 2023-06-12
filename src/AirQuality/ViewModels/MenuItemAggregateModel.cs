using System;
using AirQuality.Models;

namespace AirQuality.ViewModels;

public class MenuItemAggregateModel : ViewModelBase
{
    public string Name { get; set; } = null!;

    public TimeSpan? Window { get; set; } = null;

    public MeanType MeanType { get; set; } = MeanType.Minute;
}