using System.Text.Json.Serialization;

namespace PourCast.Providers.WeatherApi.Models;

/// <summary>
/// The forecast from WeatherAPI.com.
/// </summary>
public class WeatherApiForecast
{
    /// <summary>
    /// The forecast days.
    /// </summary>
    [JsonPropertyName("forecastday")]
    public List<WeatherApiForecastDay> ForecastDay { get; set; } = [];
}