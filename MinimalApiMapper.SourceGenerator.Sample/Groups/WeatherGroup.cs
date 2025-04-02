using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiMapper.Abstractions;

namespace MinimalApiMapper.SourceGenerator.Sample.Groups;

[MapGroup("weather")]
public class WeatherGroup(ILogger<WeatherGroup> logger, HttpClient client)
{
    [MapGet("forecast")]
    public async Task<Ok<string>> GetForecast(string latitude, string longitude)
    {
        logger.LogInformation("GetForecast: {Latitude}, {Longitude}", latitude, longitude);
        
        // Use open-meteo API to get weather forecast
        var response = await client.GetAsync(
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&hourly=temperature_2m"
        );

        return TypedResults.Ok($"Forecast for {latitude}, {longitude}");
    }
}
