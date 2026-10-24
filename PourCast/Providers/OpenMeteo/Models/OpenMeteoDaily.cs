using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenMeteo.Models;

/// <summary>
/// Daily forecast arrays from Open-Meteo.
/// </summary>
public class OpenMeteoDaily
{
    /// <summary>
    /// The local date of each day.
    /// </summary>
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    /// <summary>
    /// WMO weather code.
    /// </summary>
    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = [];

    /// <summary>
    /// Maximum air temperature at 2m.
    /// </summary>
    [JsonPropertyName("temperature_2m_max")]
    public List<double?> TemperatureMax { get; set; } = [];

    /// <summary>
    /// Minimum air temperature at 2m.
    /// </summary>
    [JsonPropertyName("temperature_2m_min")]
    public List<double?> TemperatureMin { get; set; } = [];

    /// <summary>
    /// Maximum chance of precipitation.
    /// </summary>
    [JsonPropertyName("precipitation_probability_max")]
    public List<double?> PrecipitationProbabilityMax { get; set; } = [];

    /// <summary>
    /// Total precipitation.
    /// </summary>
    [JsonPropertyName("precipitation_sum")]
    public List<double?> PrecipitationSum { get; set; } = [];

    /// <summary>
    /// Maximum wind speed at 10m.
    /// </summary>
    [JsonPropertyName("wind_speed_10m_max")]
    public List<double?> WindSpeedMax { get; set; } = [];

    /// <summary>
    /// Maximum wind gusts at 10m.
    /// </summary>
    [JsonPropertyName("wind_gusts_10m_max")]
    public List<double?> WindGustsMax { get; set; } = [];

    /// <summary>
    /// Maximum UV index.
    /// </summary>
    [JsonPropertyName("uv_index_max")]
    public List<double?> UvIndexMax { get; set; } = [];

    /// <summary>
    /// Local time of sunrise.
    /// </summary>
    [JsonPropertyName("sunrise")]
    public List<string?> Sunrise { get; set; } = [];

    /// <summary>
    /// Local time of sunset.
    /// </summary>
    [JsonPropertyName("sunset")]
    public List<string?> Sunset { get; set; } = [];
}