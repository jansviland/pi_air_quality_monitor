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

    public IEnumerable<DateTime> GetDatesWithMeasurments();
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
        // List all blobs in the container
        var containerClient = _blobServiceClient.GetBlobContainerClient("container1");
        var blobs = containerClient.GetBlobs();

        foreach (BlobItem blobItem in blobs)
        {
            // 2023/02/07/0_bc65f685d896441690ac0d2d62198e46_1.json
            // 2023/02/08/0_af12b8d2580e46af83ded32118397f5b_1.json
            // 2023/02/09/0_debc5e11ae124ee68154ff0552a2845c_1.json
            // ...
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

                _logger.LogInformation($"Downloaded file: {fullFilePath}");
            }
        }
    }

    // Go through all json files, and return a list of dates that have measurements
    public IEnumerable<DateTime> GetDatesWithMeasurments()
    {
        return _availableDates;
    }
}