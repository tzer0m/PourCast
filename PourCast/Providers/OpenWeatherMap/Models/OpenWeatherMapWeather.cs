using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenWeatherMap.Models;

/// <summary>
/// A weather condition from OpenWeatherMap.
/// </summary>
public class OpenWeatherMapWeather
{
    /// <summary>
    /// The condition ID, e.g. 500 for light rain.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }
}