using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenWeatherMap.Models;

/// <summary>
/// Temperature and humidity from OpenWeatherMap.
/// </summary>
public class OpenWeatherMapMain
{
    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    [JsonPropertyName("temp")]
    public double? Temp { get; set; }

    /// <summary>
    /// Feels like temperature in °C.
    /// </summary>
    [JsonPropertyName("feels_like")]
    public double? FeelsLike { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }
}