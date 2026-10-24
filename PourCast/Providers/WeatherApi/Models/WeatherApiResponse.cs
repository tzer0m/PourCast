using System.Text.Json.Serialization;

namespace PourCast.Providers.WeatherApi.Models;

/// <summary>
/// Response from the WeatherAPI.com forecast endpoint.
/// </summary>
public class WeatherApiResponse
{
    /// <summary>
    /// The resolved location.
    /// </summary>
    [JsonPropertyName("location")]
    public WeatherApiLocation? Location { get; set; }

    /// <summary>
    /// The forecast.
    /// </summary>
    [JsonPropertyName("forecast")]
    public WeatherApiForecast? Forecast { get; set; }
}