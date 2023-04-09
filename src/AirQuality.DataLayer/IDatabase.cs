using AirQuality.Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AirQuality.DataLayer;

public interface IDatabase
{
    public List<Measurement> GetLatestMeasurements(int count);
}

public class Database : IDatabase
{
    private readonly ILogger<Database> _logger;
    private readonly string? _connectionString;

    public Database(IConfiguration configuration, ILogger<Database> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _logger = logger;
    }

    public List<Measurement> GetLatestMeasurements(int count)
    {
        // use a stack to reverse the order of the measurements
        Stack<Measurement> measurements = new();

        using (var con = new SqlConnection(_connectionString))
        {
            con.Open();

            using (var command = new SqlCommand($"SELECT top({count}) * FROM measurements order by unixtime desc", con))
            {
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    measurements.Push(new Measurement()
                    {
                        Pm2 = reader.GetDouble(0),
                        Pm10 = reader.GetDouble(1),
                        // EventProcessedUtcTime = reader.GetDateTime(2),
                        // PartitionId = reader.GetInt64(3),
                        EventEnqueuedUtcTime = reader.GetDateTime(4),
                        // IoTHub = reader.GetString(5),

                        // unixtime can be null in the database, added it later on, so in some earlier rows it will be null
                        UnixTime = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                        ClientId = reader.GetString(7)
                    });
                }
            }
        }

        return measurements.ToList();
    }
}