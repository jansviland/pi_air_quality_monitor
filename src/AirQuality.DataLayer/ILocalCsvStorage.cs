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

            // var measurement = new Measurement
            // {
            //     Pm2 = double.Parse(values[0]),
            //     Pm10 = double.Parse(values[1]),
            //     UtcTime = DateTime.Parse(values[2]),
            //     ClientId = values[3]
            // };

            var measurement = new Measurement()
            {
                Pm2 = Convert.ToDouble(split[0], CultureInfo.InvariantCulture),
                Pm10 = Convert.ToDouble(split[1], CultureInfo.InvariantCulture),
                ClientId = split[2],

                // BUG in parsing
                // Should check format of each line, if something is wrong print an exception and continue

                // 1.0,2.1,raspberry-pi-jan,2023-04-28 19:22:30.164742
                // 1.2,1.8,raspberry-pi-jan,2023-04-28 19:23:30.624643
                // 1.1,1.8,raspberry-pi-jan,2023-04-28 19:24:31.081609
                // 1.1,1.8,raspberry-pi-jan,2023-04-28 19:25:31.542309
                // 1.2,1.9,raspberry-pi-jan,2023-04-28 19:26:32            // FAILS HERE
                // 1.2,1.7,raspberry-pi-jan,2023-04-28 19:27:32.460530
                // 1.2,2.0,raspberry-pi-jan,2023-04-28 19:28:32.925940
                // 1.1,1.9,raspberry-pi-jan,2023-04-28 19:29:33.386060
                // 1.0,1.3,raspberry-pi-jan,2023-04-28 19:30:33.845868
                // 1.0,1.6,raspberry-pi-jan,2023-04-28 19:31:34.300788
                // 1.2,1.6,raspberry-pi-jan,2023-04-28 19:32:34.762021
                // 1.1,1.7,raspberry-pi-jan,2023-04-28 19:33:35.220698
                //

                // Unhandled exception. System.FormatException: String '2023-04-28 19:26:32' was not recognized as a valid DateTime.
                //     at System.DateTime.ParseExact(String s, String format, IFormatProvider provider)
                // at AirQuality.Console.Service.Run(String[] input) in /home/pi/git/pi_air_quality_monitor/src/AirQuality.Console/IService.cs:line 40
                // at AirQuality.Console.Program.Main(String[] args) in /home/pi/git/pi_air_quality_monitor/src/AirQuality.Console/Program.cs:line 44

                UtcTime = DateTime.ParseExact(split[3], "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)
            };

            // TODO: handle parsing datetime separately

            measurements.Add(measurement);
        }

        return measurements;
    }
}