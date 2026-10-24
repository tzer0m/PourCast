using System.Text.Json.Serialization;

namespace PourCast.Providers.PirateWeather.Models;

/// <summary>
/// A block of Pirate Weather data points.
/// </summary>
public class PirateWeatherBlock
{
    /// <summary>
    /// The data points.
    /// </summary>
    [JsonPropertyName("data")]
    public List<PirateWeatherDataPoint> Data { get; set; } = [];
}