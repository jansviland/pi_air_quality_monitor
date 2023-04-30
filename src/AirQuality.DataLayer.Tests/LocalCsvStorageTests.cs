using Microsoft.Extensions.Logging;

namespace AirQuality.DataLayer.Tests;

public class LocalCsvStorageTests
{
    private readonly ILocalCsvStorage _localCsvStorage;

    private readonly string[] _input = new[]
    {
        "1.0,2.1,raspberry-pi-jan,2023-04-28 19:22:30.164742",
        "1.2,1.8,raspberry-pi-jan,2023-04-28 19:23:30.624643",
        "1.1,1.8,raspberry-pi-jan,2023-04-28 19:24:31.081609",
        "1.1,1.8,raspberry-pi-jan,2023-04-28 19:25:31.542309",
        "1.2,1.9,raspberry-pi-jan,2023-04-28 19:26:32",
        "1.2,1.7,raspberry-pi-jan,2023-04-28 19:27:32.460530",
        "1.2,2.0,raspberry-pi-jan,2023-04-28 19:28:32.925940",
        "1.1,1.9,raspberry-pi-jan,2023-04-28 19:29:33.386060",
        "1.0,1.3,raspberry-pi-jan,2023-04-28 19:30:33.845868",
        "1.0,1.6,raspberry-pi-jan,2023-04-28 19:31:34.300788",
        "1.2,1.6,raspberry-pi-jan,2023-04-28 19:32:34.762021",
        "1.1,1.7,raspberry-pi-jan,2023-04-28 19:33:35.220698"
    };

    public LocalCsvStorageTests()
    {
        var logger = A.Fake<ILogger<LocalCsvStorage>>();

        _localCsvStorage = new LocalCsvStorage(logger);
    }

    [Fact]
    public void TestParseCsvContent()
    {
        var result = _localCsvStorage.ParseCsvContent(_input);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(12, result.Count);
    }
}