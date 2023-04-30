using System.Data;
using AirQuality.Common.Extensions;
using AirQuality.Common.Models;
using AirQuality.DataLayer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AirQuality.Console;

public interface IService
{
    public void Run(string[] input);
}

public class Service : IService
{
    private readonly ILogger<Service> _logger;
    private readonly ILocalCsvStorage _localCsvStorage;
    private readonly string _connectionString;

    public Service(ILogger<Service> logger, IConfiguration configuration, ILocalCsvStorage localCsvStorage)
    {
        _logger = logger;
        _localCsvStorage = localCsvStorage;

        _connectionString = configuration.GetConnectionString("DefaultConnection")!;

        if (string.IsNullOrWhiteSpace(_connectionString) || _connectionString == "ChangeThis")
        {
            throw new ArgumentException("Missing connection string. Update appsettings.json");
        }
    }

    public void Run(string[] input)
    {
        _logger.LogInformation("Input contains {Input} values", input.Length);

        var measurements = _localCsvStorage.ParseCsvContent(input);
        BulkInsert(measurements);
    }

    private void BulkInsert(List<Measurement> measurements)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var table = new DataTable();
        table.TableName = "values";

        table.Columns.Add("Guid", typeof(Guid));
        table.Columns.Add("pm2", typeof(double));
        table.Columns.Add("pm10", typeof(double));
        table.Columns.Add("UtcTime", typeof(DateTime));
        table.Columns.Add("UnixTime", typeof(long));
        table.Columns.Add("ClientId", typeof(string));

        foreach (var measurement in measurements)
        {
            var row = table.NewRow();

            row["Guid"] = Guid.NewGuid();
            row[nameof(Measurement.Pm2)] = measurement.Pm2;
            row[nameof(Measurement.Pm10)] = measurement.Pm10;
            row[nameof(Measurement.UtcTime)] = measurement.UtcTime;
            row[nameof(Measurement.UnixTime)] = measurement.UtcTime.ToUnixTime();
            row[nameof(Measurement.ClientId)] = measurement.ClientId;

            table.Rows.Add(row);
        }

        using (var bulkInsert = new SqlBulkCopy(_connectionString))
        {
            bulkInsert.DestinationTableName = table.TableName;
            bulkInsert.WriteToServer(table);
        }
    }
}