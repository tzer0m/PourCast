using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenWeatherMap.Models;

/// <summary>
/// Cloud cover from OpenWeatherMap.
/// </summary>
public class OpenWeatherMapClouds
{
    /// <summary>
    /// Cloud cover in percent.
    /// </summary>
    [JsonPropertyName("all")]
    public double? All { get; set; }
}