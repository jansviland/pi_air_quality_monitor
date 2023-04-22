namespace AirQuality.Common.Models;

public class Measurement
{
    public double Pm2 { get; set; }
    public double Pm10 { get; set; }
    public DateTime UtcTime { get; set; }
    public long? UnixTime { get; set; }
    public string ClientId { get; set; } = null!;
}