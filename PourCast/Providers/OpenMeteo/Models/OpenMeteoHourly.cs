using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenMeteo.Models;

/// <summary>
/// Hourly forecast arrays from Open-Meteo.
/// </summary>
public class OpenMeteoHourly
{
    /// <summary>
    /// The local start time of each hour.
    /// </summary>
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    /// <summary>
    /// Air temperature at 2m.
    /// </summary>
    [JsonPropertyName("temperature_2m")]
    public List<double?> Temperature { get; set; } = [];

    /// <summary>
    /// Apparent temperature.
    /// </summary>
    [JsonPropertyName("apparent_temperature")]
    public List<double?> ApparentTemperature { get; set; } = [];

    /// <summary>
    /// Chance of precipitation.
    /// </summary>
    [JsonPropertyName("precipitation_probability")]
    public List<double?> PrecipitationProbability { get; set; } = [];

    /// <summary>
    /// Precipitation.
    /// </summary>
    [JsonPropertyName("precipitation")]
    public List<double?> Precipitation { get; set; } = [];

    /// <summary>
    /// WMO weather code.
    /// </summary>
    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = [];

    /// <summary>
    /// Relative humidity at 2m.
    /// </summary>
    [JsonPropertyName("relative_humidity_2m")]
    public List<double?> RelativeHumidity { get; set; } = [];

    /// <summary>
    /// Cloud cover.
    /// </summary>
    [JsonPropertyName("cloud_cover")]
    public List<double?> CloudCover { get; set; } = [];

    /// <summary>
    /// Wind speed at 10m.
    /// </summary>
    [JsonPropertyName("wind_speed_10m")]
    public List<double?> WindSpeed { get; set; } = [];

    /// <summary>
    /// Wind direction at 10m.
    /// </summary>
    [JsonPropertyName("wind_direction_10m")]
    public List<double?> WindDirection { get; set; } = [];

    /// <summary>
    /// Wind gusts at 10m.
    /// </summary>
    [JsonPropertyName("wind_gusts_10m")]
    public List<double?> WindGusts { get; set; } = [];

    /// <summary>
    /// UV index, allowing for cloud cover.
    /// </summary>
    [JsonPropertyName("uv_index")]
    public List<double?> UvIndex { get; set; } = [];

    /// <summary>
    /// UV index with a clear sky.
    /// </summary>
    [JsonPropertyName("uv_index_clear_sky")]
    public List<double?> UvIndexClearSky { get; set; } = [];
}