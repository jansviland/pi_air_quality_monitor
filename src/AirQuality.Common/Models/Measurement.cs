using System.Text.Json.Serialization;

namespace AirQuality.Common.Models;

public class Measurement
{
    public double Pm2 { get; set; }
    public double Pm10 { get; set; }
    // public DateTime? EventProcessedUtcTime { get; set; }
    // public long? PartitionId { get; set; }

    // TODO: rename to UtcTime?
    [JsonPropertyName("EventEnqueuedUtcTime")]
    public DateTime EventEnqueuedUtcTime { get; set; }

    // public string? IoTHub { get; set; }
    public long? UnixTime { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
}