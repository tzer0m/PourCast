using System.Text.Json.Serialization;

namespace PourCast.Providers.TomorrowIo.Models;

/// <summary>
/// Forecast values from Tomorrow.io, in metric units.
/// </summary>
public class TomorrowIoValues
{
    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Feels like temperature in °C.
    /// </summary>
    [JsonPropertyName("temperatureApparent")]
    public double? TemperatureApparent { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    /// <summary>
    /// Chance of precipitation in percent.
    /// </summary>
    [JsonPropertyName("precipitationProbability")]
    public double? PrecipitationProbability { get; set; }

    /// <summary>
    /// Rain rate in mm/h.
    /// </summary>
    [JsonPropertyName("rainIntensity")]
    public double? RainIntensity { get; set; }

    /// <summary>
    /// Freezing rain rate in mm/h.
    /// </summary>
    [JsonPropertyName("freezingRainIntensity")]
    public double? FreezingRainIntensity { get; set; }

    /// <summary>
    /// Sleet rate in mm/h.
    /// </summary>
    [JsonPropertyName("sleetIntensity")]
    public double? SleetIntensity { get; set; }

    /// <summary>
    /// Snow rate in mm/h.
    /// </summary>
    [JsonPropertyName("snowIntensity")]
    public double? SnowIntensity { get; set; }

    /// <summary>
    /// Wind speed in m/s.
    /// </summary>
    [JsonPropertyName("windSpeed")]
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind gust speed in m/s.
    /// </summary>
    [JsonPropertyName("windGust")]
    public double? WindGust { get; set; }

    /// <summary>
    /// Direction the wind is coming from in degrees.
    /// </summary>
    [JsonPropertyName("windDirection")]
    public double? WindDirection { get; set; }

    /// <summary>
    /// Cloud cover in percent.
    /// </summary>
    [JsonPropertyName("cloudCover")]
    public double? CloudCover { get; set; }

    /// <summary>
    /// UV index.
    /// </summary>
    [JsonPropertyName("uvIndex")]
    public double? UvIndex { get; set; }

    /// <summary>
    /// Tomorrow.io weather code.
    /// </summary>
    [JsonPropertyName("weatherCode")]
    public int? WeatherCode { get; set; }
}