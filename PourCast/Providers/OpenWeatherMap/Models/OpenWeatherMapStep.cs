using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenWeatherMap.Models;

/// <summary>
/// A single 3 hour step from OpenWeatherMap.
/// </summary>
public class OpenWeatherMapStep
{
    /// <summary>
    /// The time of the step as a Unix timestamp.
    /// </summary>
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    /// <summary>
    /// Temperature and humidity at the step.
    /// </summary>
    [JsonPropertyName("main")]
    public OpenWeatherMapMain? Main { get; set; }

    /// <summary>
    /// The weather conditions.
    /// </summary>
    [JsonPropertyName("weather")]
    public List<OpenWeatherMapWeather> Weather { get; set; } = [];

    /// <summary>
    /// Cloud cover at the step.
    /// </summary>
    [JsonPropertyName("clouds")]
    public OpenWeatherMapClouds? Clouds { get; set; }

    /// <summary>
    /// Wind at the step.
    /// </summary>
    [JsonPropertyName("wind")]
    public OpenWeatherMapWind? Wind { get; set; }

    /// <summary>
    /// Chance of precipitation over the 3 hours, as a fraction from 0 to 1.
    /// </summary>
    [JsonPropertyName("pop")]
    public double? Pop { get; set; }

    /// <summary>
    /// Rain over the 3 hours.
    /// </summary>
    [JsonPropertyName("rain")]
    public OpenWeatherMapVolume? Rain { get; set; }

    /// <summary>
    /// Snow over the 3 hours.
    /// </summary>
    [JsonPropertyName("snow")]
    public OpenWeatherMapVolume? Snow { get; set; }
}