using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AirQuality.Common.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AirQuality.DataLayer;

public interface IBlobStorage
{
    /// <summary>
    /// This method should be called on startup to update the local files and update the available dates
    /// </summary>
    public void UpdateLocalFiles();

    public List<DateTime> GetDatesWithMeasurments();
    public List<Measurement> GetMeasurementsForDate(DateTime dateTime);
    public bool HasMeasurementsForDate(DateTime dateTime);
}

public class BlobStorage : IBlobStorage
{
    private readonly ILogger<BlobStorage> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    private readonly List<DateTime> _availableDates = new List<DateTime>();

    // in order to be cross platform, support both / and \ folder seperators
    private readonly char _slash = Path.DirectorySeparatorChar;

    public BlobStorage(IConfiguration configuration, ILogger<BlobStorage> logger)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("BlobStorageConnectionString");
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public void UpdateLocalFiles()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("container1");
        var blobs = containerClient.GetBlobs();

        foreach (BlobItem blobItem in blobs)
        {
            // 2023/02/07/0_bc65f685d896441690ac0d2d62198e46_1.json
            // 2023/02/08/0_af12b8d2580e46af83ded32118397f5b_1.json
            // 2023/02/09/0_debc5e11ae124ee68154ff0552a2845c_1.json
            // (...)
            // 2023/04/05/0_3584554115e649f2ae93a9aa005702fe_1.json

            _logger.LogInformation("Found blob {BlobName}", blobItem.Name);

            var split = blobItem.Name.Split('/');
            var year = split[0];
            var month = split[1];
            var day = split[2];
            var filename = split[3];
            var currentDirectory = Directory.GetCurrentDirectory();

            // update list of available dates
            _availableDates.Add(new DateTime(year: int.Parse(year), month: int.Parse(month), day: int.Parse(day)));

            var fullFilePath = $"{currentDirectory}{_slash}BlobStorage{_slash}{year}{_slash}{month}{_slash}{day}{_slash}{filename}";

            var exist = File.Exists(fullFilePath);
            if (exist)
            {
                _logger.LogInformation("Found file: {FullFilePath}", fullFilePath);
            }
            else
            {
                _logger.LogInformation("Downloading file: {FullFilePath}", fullFilePath);

                // Create directory if it does not exist
                var directory = Path.GetDirectoryName(fullFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                    _logger.LogInformation("Created directory: {Directory}", directory);
                }

                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                blobClient.DownloadTo(fullFilePath);

                _logger.LogInformation("Downloaded file: {FullFilePath}", fullFilePath);
            }
        }
    }

    // Go through all json files, and return a list of dates that have measurements
    public List<DateTime> GetDatesWithMeasurments()
    {
        return _availableDates;
    }

    public List<Measurement> GetMeasurementsForDate(DateTime dateTime)
    {
        if (!_availableDates.Contains(dateTime))
        {
            // you can select a date with no measurements, and it will return an empty list
            _logger.LogError("No measurements found for date {DateTime}", dateTime);
            return new List<Measurement>();

            // _logger.LogError("No measurements found for date {DateTime}", dateTime);
            // throw new Exception($"No measurements found for date {dateTime}");
        }

        var year = dateTime.Year.ToString();
        var month = dateTime.Month.ToString("d2");
        var day = dateTime.Day.ToString("d2");
        var currentDirectory = Directory.GetCurrentDirectory();

        var directory = $"{currentDirectory}{_slash}BlobStorage{_slash}{year}{_slash}{month}{_slash}{day}";

        if (!Directory.Exists(directory))
        {
            _logger.LogError("Folder {Directory} does not exist", directory);
            throw new Exception($"Folder {directory} does not exist");
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            _logger.LogError("No files found in {Directory}", directory);
            throw new Exception($"No files found in {directory}");
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
            // Console.WriteLine(e);
            _logger.LogError(e, "Could not deserialize json");
            // throw;
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