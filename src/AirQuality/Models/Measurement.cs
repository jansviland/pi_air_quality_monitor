using System;

namespace AirQuality.Models;

public class Measurement
{
    public double Pm2 { get; set; }
    public double Pm10 { get; set; }
    public DateTime? EventProcessedUtcTime { get; set; }
    public long? PartitionId { get; set; }
    public DateTime EventEnqueuedUtcTime { get; set; }
    public string? IoTHub { get; set; }
    public long? UnixTime { get; set; }
    public string? ClientId { get; set; }
}