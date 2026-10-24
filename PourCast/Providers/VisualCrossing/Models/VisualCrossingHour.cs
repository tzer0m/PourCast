using System.Text.Json.Serialization;

namespace PourCast.Providers.VisualCrossing.Models;

/// <summary>
/// A single hour from Visual Crossing, in UK units.
/// </summary>
public class VisualCrossingHour
{
    /// <summary>
    /// The start of the hour as a Unix timestamp.
    /// </summary>
    [JsonPropertyName("datetimeEpoch")]
    public long DatetimeEpoch { get; set; }

    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    [JsonPropertyName("temp")]
    public double? Temp { get; set; }

    /// <summary>
    /// Feels like temperature in °C.
    /// </summary>
    [JsonPropertyName("feelslike")]
    public double? FeelsLike { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    /// <summary>
    /// Precipitation in mm.
    /// </summary>
    [JsonPropertyName("precip")]
    public double? Precip { get; set; }

    /// <summary>
    /// Chance of precipitation in percent.
    /// </summary>
    [JsonPropertyName("precipprob")]
    public double? PrecipProb { get; set; }

    /// <summary>
    /// Wind speed in mph.
    /// </summary>
    [JsonPropertyName("windspeed")]
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind gust speed in mph.
    /// </summary>
    [JsonPropertyName("windgust")]
    public double? WindGust { get; set; }

    /// <summary>
    /// Direction the wind is coming from in degrees.
    /// </summary>
    [JsonPropertyName("winddir")]
    public double? WindDir { get; set; }

    /// <summary>
    /// Cloud cover in percent.
    /// </summary>
    [JsonPropertyName("cloudcover")]
    public double? CloudCover { get; set; }

    /// <summary>
    /// UV index.
    /// </summary>
    [JsonPropertyName("uvindex")]
    public double? UvIndex { get; set; }

    /// <summary>
    /// The weather icon name, e.g. partly-cloudy-day.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}