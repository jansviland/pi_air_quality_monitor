namespace AirQuality.Console;

public interface IService
{
    public int Run(string[] input);
}

public class Service : IService
{
    private readonly ILogger<Service> _logger;

    public Service(ILogger<Service> logger)
    {
        _logger = logger;
    }

    public int Run(string[] input)
    {
        _logger.LogInformation("Input contains {Input} values", input.Length);

        throw new NotImplementedException();
    }
}