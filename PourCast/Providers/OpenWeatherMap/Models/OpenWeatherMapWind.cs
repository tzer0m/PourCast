using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenWeatherMap.Models;

/// <summary>
/// Wind from OpenWeatherMap, in metric units.
/// </summary>
public class OpenWeatherMapWind
{
    /// <summary>
    /// Wind speed in m/s.
    /// </summary>
    [JsonPropertyName("speed")]
    public double? Speed { get; set; }

    /// <summary>
    /// Direction the wind is coming from in degrees.
    /// </summary>
    [JsonPropertyName("deg")]
    public double? Deg { get; set; }

    /// <summary>
    /// Wind gust speed in m/s.
    /// </summary>
    [JsonPropertyName("gust")]
    public double? Gust { get; set; }
}