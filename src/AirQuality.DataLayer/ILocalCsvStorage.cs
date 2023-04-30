using System.Diagnostics.Metrics;
using System.Globalization;
using AirQuality.Common.Models;
using Microsoft.Extensions.Logging;

namespace AirQuality.DataLayer;

public interface ILocalCsvStorage
{
    public List<Measurement> ParseCsvContent(string[] csvContent);
}

public class LocalCsvStorage : ILocalCsvStorage
{
    private readonly ILogger<LocalCsvStorage> _logger;

    public LocalCsvStorage(ILogger<LocalCsvStorage> logger)
    {
        _logger = logger;
    }

    public List<Measurement> ParseCsvContent(string[] csvContent)
    {
        var measurements = new List<Measurement>();

        foreach (var line in csvContent)
        {
            var split = line.Split(',');
            if (split.Length != 4)
            {
                _logger.LogWarning("Invalid line in csv file: {Line}", line);
                continue;
            }

            var measurement = new Measurement()
            {
                Pm2 = Convert.ToDouble(split[0], CultureInfo.InvariantCulture),
                Pm10 = Convert.ToDouble(split[1], CultureInfo.InvariantCulture),
                ClientId = split[2],
            };

            // handle different date formats
            var valid = DateTime.TryParseExact(split[3], "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt);
            if (!valid)
            {
                _logger.LogWarning("Invalid date format in csv file: {Line}", line);

                var dt2 = DateTime.ParseExact(split[3], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); // format example: 2023-04-28 19:26:32
                measurement.UtcTime = dt2;
                measurement.UnixTime = new DateTimeOffset(dt2).ToUnixTimeSeconds();
            }
            else
            {
                measurement.UtcTime = dt; // format example: 2023-04-28 19:26:32.460530
                measurement.UnixTime = new DateTimeOffset(dt).ToUnixTimeSeconds();
            }

            measurements.Add(measurement);
        }

        return measurements;
    }
}