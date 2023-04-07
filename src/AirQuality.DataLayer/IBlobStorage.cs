using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AirQuality.DataLayer;

public interface IBlobStorage
{
    public void DownloadFiles();
    public IEnumerable<DateTime> GetDatesWithMeasurments();
}

public class BlobStorage : IBlobStorage
{
    private readonly ILogger<BlobStorage> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorage(IConfiguration configuration, ILogger<BlobStorage> logger)
    {
        _logger = logger;

        var storageAccountName = configuration["BlobStorage:StorageAccountName"];
        _blobServiceClient = new BlobServiceClient(
            new Uri($"https://{storageAccountName}.blob.core.windows.net"),
            new DefaultAzureCredential());
    }

    public void DownloadFiles()
    {
        throw new NotImplementedException();
    }

    // Go through all json files, and return a list of dates that have measurements
    public IEnumerable<DateTime> GetDatesWithMeasurments()
    {
        throw new NotImplementedException();
    }
}