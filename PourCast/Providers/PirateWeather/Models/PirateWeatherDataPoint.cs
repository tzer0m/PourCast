using System.Text.Json.Serialization;

namespace PourCast.Providers.PirateWeather.Models;

/// <summary>
/// A single hourly Pirate Weather data point, in UK units.
/// </summary>
public class PirateWeatherDataPoint
{
    /// <summary>
    /// The start of the hour as a Unix timestamp.
    /// </summary>
    [JsonPropertyName("time")]
    public long Time { get; set; }

    /// <summary>
    /// The weather icon name, e.g. partly-cloudy-day.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Feels like temperature in °C.
    /// </summary>
    [JsonPropertyName("apparentTemperature")]
    public double? ApparentTemperature { get; set; }

    /// <summary>
    /// Relative humidity as a fraction from 0 to 1.
    /// </summary>
    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    /// <summary>
    /// Precipitation rate in mm/h.
    /// </summary>
    [JsonPropertyName("precipIntensity")]
    public double? PrecipIntensity { get; set; }

    /// <summary>
    /// Chance of precipitation as a fraction from 0 to 1.
    /// </summary>
    [JsonPropertyName("precipProbability")]
    public double? PrecipProbability { get; set; }

    /// <summary>
    /// Wind speed in mph.
    /// </summary>
    [JsonPropertyName("windSpeed")]
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind gust speed in mph.
    /// </summary>
    [JsonPropertyName("windGust")]
    public double? WindGust { get; set; }

    /// <summary>
    /// Direction the wind is coming from in degrees.
    /// </summary>
    [JsonPropertyName("windBearing")]
    public double? WindBearing { get; set; }

    /// <summary>
    /// Cloud cover as a fraction from 0 to 1.
    /// </summary>
    [JsonPropertyName("cloudCover")]
    public double? CloudCover { get; set; }

    /// <summary>
    /// UV index.
    /// </summary>
    [JsonPropertyName("uvIndex")]
    public double? UvIndex { get; set; }
}