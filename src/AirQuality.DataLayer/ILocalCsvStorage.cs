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

        for (var i = 1; i < csvContent.Length; i++)
        {
            var line = csvContent[i];

            var split = line.Split(',');
            if (split.Length != 4)
            {
                _logger.LogWarning("Invalid line in csv file: {Line}", line);
                continue;
            }

            var measurement = new Measurement()
            {
                // Pm2 = Convert.ToDouble(split[0], CultureInfo.InvariantCulture),
                // Pm10 = Convert.ToDouble(split[1], CultureInfo.InvariantCulture),
                ClientId = split[2].Trim(),
            };

            var validPm2 = double.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var pm2);
            if (!validPm2)
            {
                _logger.LogWarning("Invalid pm2 value in csv file: {Line}", line);
                continue;
            }

            measurement.Pm2 = pm2;

            var validPm10 = double.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var pm10);
            if (!validPm10)
            {
                _logger.LogWarning("Invalid pm10 value in csv file: {Line}", line);
                continue;
            }

            measurement.Pm10 = pm10;

            // handle different date formats
            var validDate = DateTime.TryParseExact(split[3], "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt);
            if (!validDate)
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