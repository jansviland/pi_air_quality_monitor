using System.Text.Json;
using AirQuality.Common.Models;
using Microsoft.Extensions.Logging;

namespace AirQuality.DataLayer;

public interface ILocalStorage
{
    // TODO: get measurements for specific client ex. "raspberry-pi-jan"
    public List<DateTime> GetDatesWithMeasurments();

    // TODO: get measurements for specific client ex. "raspberry-pi-jan"
    public List<Measurement>? GetMeasurementsForDate(DateTime dateTime);

    // TODO: get measurements for specific client ex. "raspberry-pi-jan"
    public bool HasMeasurementsForDate(DateTime dateTime);

    // TODO: get measurements for specific client ex. "raspberry-pi-jan"
    public void SaveMeasurementsForDate(DateTime dateTime, List<Measurement> measurements);
}

public class LocalStorage : ILocalStorage
{
    private readonly ILogger<LocalStorage> _logger;

    // in order to be cross platform, support both / and \ folder seperators
    private readonly char _slash = Path.DirectorySeparatorChar;
    private readonly string _folderName = "storage";

    private List<DateTime>? _availableDates = new List<DateTime>();

    public LocalStorage(ILogger<LocalStorage> logger)
    {
        _logger = logger;
    }

    // go through all files in the local storage folder, check year, month, day folders, and check if file exist in that folder
    public List<DateTime> GetDatesWithMeasurments()
    {
        var datesWithMeasurements = new List<DateTime>();
        var currentDirectory = Directory.GetCurrentDirectory();

        if (!Directory.Exists($"{currentDirectory}{_slash}{_folderName}"))
        {
            _logger.LogInformation("No local storage folder found, creating new one");
            Directory.CreateDirectory($"{currentDirectory}{_slash}{_folderName}");

            return datesWithMeasurements;
        }

        var rootDirectoryPath = $"{currentDirectory}{_slash}{_folderName}";
        var folder = new DirectoryInfo(rootDirectoryPath);
        var subFoldersYears = folder.GetDirectories();

        foreach (var foldersYear in subFoldersYears)
        {
            var subFoldersMonths = foldersYear.GetDirectories();
            foreach (var foldersMonth in subFoldersMonths)
            {
                var subFoldersDays = foldersMonth.GetDirectories();
                foreach (var foldersDay in subFoldersDays)
                {
                    var files = foldersDay.GetFiles();
                    if (files.Length > 0)
                    {
                        var year = int.Parse(foldersYear.Name);
                        var month = int.Parse(foldersMonth.Name);
                        var day = int.Parse(foldersDay.Name);

                        datesWithMeasurements.Add(new DateTime(year, month, day));
                    }
                }
            }
        }

        _availableDates = datesWithMeasurements;

        return _availableDates;
    }

    public List<Measurement>? GetMeasurementsForDate(DateTime dateTime)
    {
        if (!_availableDates.Contains(dateTime))
        {
            _logger.LogError("No measurements found for date {DateTime}", dateTime);
            return null;
        }

        var year = dateTime.Year.ToString();
        var month = dateTime.Month.ToString("d2");
        var day = dateTime.Day.ToString("d2");
        var currentDirectory = Directory.GetCurrentDirectory();

        var directory = $"{currentDirectory}{_slash}{_folderName}{_slash}{year}{_slash}{month}{_slash}{day}";

        if (!Directory.Exists(directory))
        {
            _logger.LogInformation("Folder {Directory} does not exist", directory);
            return null;
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            _logger.LogInformation("No files found in {Directory}", directory);
            return null;
        }

        // order by last modified, sometimes we retieve the json file before it is fully written
        // then it misses a closing bracket, and the deserialization fails. When we later retrieve the file, it is fully written.
        // with the closing bracket. Make sure we always retrieve the file that is the latest modified.
        files = files.OrderBy(f => new FileInfo(f).LastWriteTime).ToArray();

        var json = File.ReadAllText(files.Last());

        try
        {
            // TODO: measurements fail when trying to deserialize the last file, since it has not added a closing bracket yet.
            // this is added at the end of each day.
            var measurements = JsonSerializer.Deserialize<List<Measurement>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            return measurements;
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Could not deserialize json");
            // throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }

        // if (measurements == null)
        // {
        //     throw new Exception($"Could not deserialize json");
        // }

        return new List<Measurement>();
    }

    public bool HasMeasurementsForDate(DateTime dateTime)
    {
        var year = dateTime.Year.ToString();
        var month = dateTime.Month.ToString("d2");
        var day = dateTime.Day.ToString("d2");
        var currentDirectory = Directory.GetCurrentDirectory();

        var directory = $"{currentDirectory}{_slash}{_folderName}{_slash}{year}{_slash}{month}{_slash}{day}";

        if (!Directory.Exists(directory))
        {
            return false;
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            return false;
        }

        // var json = File.ReadAllText(files.First());
        //
        // try
        // {
        //     var measurements = JsonSerializer.Deserialize<List<Measurement>>(json, new JsonSerializerOptions
        //     {
        //         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //     });
        //
        //     if (measurements == null)
        //     {
        //         return false;
        //     }
        // }
        // catch (Exception e)
        // {
        //     _logger.LogError(e, $"Could not deserialize json for {dateTime}");
        //     return false;
        // }

        return true;
    }

    public void SaveMeasurementsForDate(DateTime dateTime, List<Measurement> measurements)
    {
        var year = dateTime.Year.ToString();
        var month = dateTime.Month.ToString("d2");
        var day = dateTime.Day.ToString("d2");

        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = $"{currentDirectory}{_slash}{_folderName}{_slash}{year}{_slash}{month}{_slash}{day}";

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // save json
        var json = JsonSerializer.Serialize(measurements, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });

        var fileName = $"{dateTime:yyyy-MM-dd}-minute-interval.json";
        var filePath = $"{directory}{_slash}{fileName}";

        File.WriteAllText(filePath, json);
    }
}