using System.Text.Json.Serialization;

namespace AirQuality.Common.Models;

public class Measurement
{
    public double Pm2 { get; set; }
    public double Pm10 { get; set; }

    [JsonPropertyName("UtcTime")]
    public DateTime UtcTime { get; set; }
    public long? UnixTime { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
}