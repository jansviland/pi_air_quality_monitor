using System.Data;
using System.Globalization;
using AirQuality.Common.Extensions;
using AirQuality.Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AirQuality.Console;

public interface IService
{
    public void Run(string[] input, string? connectionString = null);
}

public class Service : IService
{
    private readonly ILogger<Service> _logger;
    private readonly string _connectionString;

    public Service(ILogger<Service> logger, IConfiguration configuration)
    {
        _logger = logger;

        _connectionString = configuration.GetConnectionString("DefaultConnection")!;

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new ArgumentException("Connection string is empty");
        }
    }

    public void Run(string[] input, string? connectionString = null)
    {
        _logger.LogInformation("Input contains {Input} values", input.Length);

        var measurements = new List<Measurement>();
        for (var i = 1; i < input.Length; i++)
        {
            var split = input[i].Split(',');
            measurements.Add(new Measurement()
            {
                Pm2 = Convert.ToDouble(split[0], CultureInfo.InvariantCulture),
                Pm10 = Convert.ToDouble(split[1], CultureInfo.InvariantCulture),
                ClientId = split[2],
                EventEnqueuedUtcTime = DateTime.ParseExact(split[3], "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)
            });
        }

        BulkInsert(measurements, connectionString ?? _connectionString);
    }

    private void BulkInsert(List<Measurement> measurements, string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        DataTable table = new DataTable();
        // table.TableName = "measurements";
        table.TableName = "values";

        table.Columns.Add("Guid", typeof(Guid));
        table.Columns.Add("pm2", typeof(double));
        table.Columns.Add("pm10", typeof(double));
        // table.Columns.Add("EventProcessedUtcTime", typeof(DateTime));
        // table.Columns.Add("PartitionId", typeof(long));
        // table.Columns.Add("EventEnqueuedUtcTime", typeof(DateTime));
        table.Columns.Add("UtcTime", typeof(DateTime));
        // table.Columns.Add("IoTHub", typeof(string));
        table.Columns.Add("UnixTime", typeof(long));
        table.Columns.Add("ClientId", typeof(string));

        foreach (var measurement in measurements)
        {
            var row = table.NewRow();

            row["Guid"] = Guid.NewGuid();
            row[nameof(Measurement.Pm2)] = measurement.Pm2;
            row[nameof(Measurement.Pm10)] = measurement.Pm10;
            // row[nameof(Measurement.EventProcessedUtcTime)] = GetDBValue(measurement.EventProcessedUtcTime);
            // row[nameof(Measurement.PartitionId)] = GetDBValue(measurement.PartitionId);
            // row[nameof(Measurement.EventEnqueuedUtcTime)] = measurement.EventEnqueuedUtcTime;
            row["UtcTime"] = measurement.EventEnqueuedUtcTime;
            // row[nameof(Measurement.IoTHub)] = GetDBValue(measurement.IoTHub);
            row[nameof(Measurement.UnixTime)] = measurement.EventEnqueuedUtcTime.ToUnixTime();
            row[nameof(Measurement.ClientId)] = measurement.ClientId;

            table.Rows.Add(row);
        }

        using (var bulkInsert = new SqlBulkCopy(_connectionString))
        {
            bulkInsert.DestinationTableName = table.TableName;
            bulkInsert.WriteToServer(table);
        }
    }

    // private static object GetDBValue(object? o)
    // {
    //     return o ?? (object)DBNull.Value;
    // }
}