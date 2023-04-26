using AirQuality.Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AirQuality.DataLayer;

public interface IDatabase
{
    // TODO: get measurements for specific client ex. "raspberry-pi-jan"
    public List<DateTime> GetDatesWithMeasurments();

    // TODO: get measurements for specific client ex. "raspberry-pi-jan"
    public List<Measurement> GetLatestMeasurements(int count);

    // TODO: get measurements for specific client ex. "raspberry-pi-jan"
    public List<Measurement>? GetMeasurementsForDate(DateTime dateTime);

    // TODO: get measurements for specific client ex. "raspberry-pi-jan"
    public bool HasMeasurementsForDate(DateTime dateTime);
}

public class Database : IDatabase
{
    private readonly ILogger<Database> _logger;
    private readonly string? _connectionString;

    private List<DateTime>? _availableDates;

    public Database(IConfiguration configuration, ILogger<Database> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _logger = logger;
    }

    public List<DateTime> GetDatesWithMeasurments()
    {
        var datesWithMeasurements = new List<DateTime>();

        const string sql = "SELECT DISTINCT CAST(UtcTime as date) as date FROM [dbo].[values] order by date";
        using (var con = new SqlConnection(_connectionString))
        {
            con.Open();

            using (var command = new SqlCommand(sql, con))
            {
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    datesWithMeasurements.Add(reader.GetDateTime(0));
                }
            }
        }

        _availableDates = datesWithMeasurements;
        return _availableDates;
    }

    public List<Measurement>? GetMeasurementsForDate(DateTime dateTime)
    {
        var measurements = new List<Measurement>();

        using (var con = new SqlConnection(_connectionString))
        {
            con.Open();

            var sql = $"SELECT pm2, pm10, UtcTime, UnixTime, ClientId FROM [dbo].[values] where CAST(UtcTime as date) = '{dateTime:yyyy-MM-dd}'";

            using (var command = new SqlCommand(sql, con))
            {
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    measurements.Add(new Measurement()
                    {
                        Pm2 = reader.GetDouble(0),
                        Pm10 = reader.GetDouble(1),
                        UtcTime = reader.GetDateTime(2),
                        UnixTime = reader.GetInt64(3),
                        ClientId = reader.GetString(4)
                    });
                }
            }
        }

        _logger.LogInformation($"Found {measurements.Count} measurements for date {dateTime:yyyy-MM-dd} in the database.");

        return measurements;
    }

    public bool HasMeasurementsForDate(DateTime dateTime)
    {
        if (_availableDates == null)
        {
            GetDatesWithMeasurments();
        }

        return _availableDates!.Contains(dateTime);
    }

    public List<Measurement> GetLatestMeasurements(int count)
    {
        // use a stack to reverse the order of the measurements
        Stack<Measurement> measurements = new();

        using (var con = new SqlConnection(_connectionString))
        {
            con.Open();

            var sql = $"SELECT top({count}) * FROM measurements order by unixtime desc";
            // var sql = $"SELECT top({count}) * FROM [values] order by unixtime desc";

            using (var command = new SqlCommand(sql, con))
            {
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    measurements.Push(new Measurement()
                    {
                        Pm2 = reader.GetDouble(0),
                        Pm10 = reader.GetDouble(1),
                        UtcTime = reader.GetDateTime(4),

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