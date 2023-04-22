using System.Text.Json;
using AirQuality.Common.Models;
using Microsoft.Extensions.Logging;

namespace AirQuality.DataLayer;

public interface ILocalStorage
{
    public List<DateTime> GetDatesWithMeasurments();
    public List<Measurement>? GetMeasurementsForDate(DateTime dateTime);
    public bool HasMeasurementsForDate(DateTime dateTime);
}

public class LocalStorage : ILocalStorage
{
    private readonly ILogger<LocalStorage> _logger;

    // in order to be cross platform, support both / and \ folder seperators
    private readonly char _slash = Path.DirectorySeparatorChar;
    private readonly string _folderName = "BlobStorage";

    private List<DateTime>? _availableDates;

    public LocalStorage(ILogger<LocalStorage> logger)
    {
        _logger = logger;
    }

    public List<DateTime> GetDatesWithMeasurments()
    {
        // TODO: go through all files in the local storage folder, check year, month, day folders, and check if file exist in that folder

        var datesWithMeasurements = new List<DateTime>();
        var currentDirectory = Directory.GetCurrentDirectory();

        // var directory = $"{currentDirectory}{_slash}{_folderName}{_slash}{year}{_slash}{month}{_slash}{day}";
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
            // you can select a date with no measurements, and it will return an empty list
            _logger.LogError("No measurements found for date {DateTime}", dateTime);
            // return new List<Measurement>();
            return null;

            // _logger.LogError("No measurements found for date {DateTime}", dateTime);
            // throw new Exception($"No measurements found for date {dateTime}");
        }

        var year = dateTime.Year.ToString();
        var month = dateTime.Month.ToString("d2");
        var day = dateTime.Day.ToString("d2");
        var currentDirectory = Directory.GetCurrentDirectory();

        var directory = $"{currentDirectory}{_slash}{_folderName}{_slash}{year}{_slash}{month}{_slash}{day}";

        if (!Directory.Exists(directory))
        {
            _logger.LogInformation("Folder {Directory} does not exist", directory);
            // throw new Exception($"Folder {directory} does not exist");
            return null;
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            _logger.LogInformation("No files found in {Directory}", directory);
            // throw new Exception($"No files found in {directory}");
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

        var directory = $"{currentDirectory}{_slash}BlobStorage{_slash}{year}{_slash}{month}{_slash}{day}";

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
        // var measurements = JsonSerializer.Deserialize<List<Measurement>>(json, new JsonSerializerOptions
        // {
        //     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // });
        //
        // if (measurements == null)
        // {
        //     return false;
        // }

        return true;
    }
}