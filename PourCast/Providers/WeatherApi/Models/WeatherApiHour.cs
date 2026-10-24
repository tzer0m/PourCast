using System.Text.Json.Serialization;

namespace PourCast.Providers.WeatherApi.Models;

/// <summary>
/// A single hour from WeatherAPI.com.
/// </summary>
public class WeatherApiHour
{
    /// <summary>
    /// The start of the hour as a Unix timestamp.
    /// </summary>
    [JsonPropertyName("time_epoch")]
    public long TimeEpoch { get; set; }

    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    [JsonPropertyName("temp_c")]
    public double? TempC { get; set; }

    /// <summary>
    /// Feels like temperature in °C.
    /// </summary>
    [JsonPropertyName("feelslike_c")]
    public double? FeelsLikeC { get; set; }

    /// <summary>
    /// The weather condition.
    /// </summary>
    [JsonPropertyName("condition")]
    public WeatherApiCondition? Condition { get; set; }

    /// <summary>
    /// Wind speed in mph.
    /// </summary>
    [JsonPropertyName("wind_mph")]
    public double? WindMph { get; set; }

    /// <summary>
    /// Wind gust speed in mph.
    /// </summary>
    [JsonPropertyName("gust_mph")]
    public double? GustMph { get; set; }

    /// <summary>
    /// Direction the wind is coming from in degrees.
    /// </summary>
    [JsonPropertyName("wind_degree")]
    public double? WindDegree { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    /// <summary>
    /// Cloud cover in percent.
    /// </summary>
    [JsonPropertyName("cloud")]
    public double? Cloud { get; set; }

    /// <summary>
    /// Precipitation in mm.
    /// </summary>
    [JsonPropertyName("precip_mm")]
    public double? PrecipMm { get; set; }

    /// <summary>
    /// Chance of rain in percent.
    /// </summary>
    [JsonPropertyName("chance_of_rain")]
    public double? ChanceOfRain { get; set; }

    /// <summary>
    /// Chance of snow in percent.
    /// </summary>
    [JsonPropertyName("chance_of_snow")]
    public double? ChanceOfSnow { get; set; }

    /// <summary>
    /// UV index.
    /// </summary>
    [JsonPropertyName("uv")]
    public double? Uv { get; set; }
}