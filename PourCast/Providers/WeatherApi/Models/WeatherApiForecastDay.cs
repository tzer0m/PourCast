using System.Text.Json.Serialization;

namespace PourCast.Providers.WeatherApi.Models;

/// <summary>
/// A single forecast day from WeatherAPI.com.
/// </summary>
public class WeatherApiForecastDay
{
    /// <summary>
    /// The hours in the day.
    /// </summary>
    [JsonPropertyName("hour")]
    public List<WeatherApiHour> Hour { get; set; } = [];
}