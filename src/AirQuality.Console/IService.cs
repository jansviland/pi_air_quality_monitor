using System.Data;
using System.Globalization;
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
        // for (var i = 1; i < input.Length; i++)
        // {
        //     var split = input[i].Split(',');
        //     measurements.Add(new Measurement()
        //     {
        //         Pm2 = Convert.ToDouble(split[0], CultureInfo.InvariantCulture),
        //         Pm10 = Convert.ToDouble(split[1], CultureInfo.InvariantCulture),
        //         ClientId = split[2],
        //
        //         // BUG in parsing
        //         // Should check format of each line, if something is wrong print an exception and continue
        //
        //         // 1.0,2.1,raspberry-pi-jan,2023-04-28 19:22:30.164742
        //         // 1.2,1.8,raspberry-pi-jan,2023-04-28 19:23:30.624643
        //         // 1.1,1.8,raspberry-pi-jan,2023-04-28 19:24:31.081609
        //         // 1.1,1.8,raspberry-pi-jan,2023-04-28 19:25:31.542309
        //         // 1.2,1.9,raspberry-pi-jan,2023-04-28 19:26:32            // FAILS HERE
        //         // 1.2,1.7,raspberry-pi-jan,2023-04-28 19:27:32.460530
        //         // 1.2,2.0,raspberry-pi-jan,2023-04-28 19:28:32.925940
        //         // 1.1,1.9,raspberry-pi-jan,2023-04-28 19:29:33.386060
        //         // 1.0,1.3,raspberry-pi-jan,2023-04-28 19:30:33.845868
        //         // 1.0,1.6,raspberry-pi-jan,2023-04-28 19:31:34.300788
        //         // 1.2,1.6,raspberry-pi-jan,2023-04-28 19:32:34.762021
        //         // 1.1,1.7,raspberry-pi-jan,2023-04-28 19:33:35.220698
        //         //
        //
        //         // Unhandled exception. System.FormatException: String '2023-04-28 19:26:32' was not recognized as a valid DateTime.
        //         //     at System.DateTime.ParseExact(String s, String format, IFormatProvider provider)
        //         // at AirQuality.Console.Service.Run(String[] input) in /home/pi/git/pi_air_quality_monitor/src/AirQuality.Console/IService.cs:line 40
        //         // at AirQuality.Console.Program.Main(String[] args) in /home/pi/git/pi_air_quality_monitor/src/AirQuality.Console/Program.cs:line 44
        //
        //         UtcTime = DateTime.ParseExact(split[3], "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)
        //     });
        // }

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