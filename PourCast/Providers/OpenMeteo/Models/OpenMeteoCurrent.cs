using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenMeteo.Models;

/// <summary>
/// Current conditions block from Open-Meteo.
/// </summary>
public class OpenMeteoCurrent
{
    /// <summary>
    /// The local time of the observation.
    /// </summary>
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// Air temperature at 2m.
    /// </summary>
    [JsonPropertyName("temperature_2m")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Apparent temperature.
    /// </summary>
    [JsonPropertyName("apparent_temperature")]
    public double? ApparentTemperature { get; set; }

    /// <summary>
    /// Relative humidity at 2m.
    /// </summary>
    [JsonPropertyName("relative_humidity_2m")]
    public double? RelativeHumidity { get; set; }

    /// <summary>
    /// Whether it is daytime (1) or night (0).
    /// </summary>
    [JsonPropertyName("is_day")]
    public int? IsDay { get; set; }

    /// <summary>
    /// Precipitation.
    /// </summary>
    [JsonPropertyName("precipitation")]
    public double? Precipitation { get; set; }

    /// <summary>
    /// WMO weather code.
    /// </summary>
    [JsonPropertyName("weather_code")]
    public int? WeatherCode { get; set; }

    /// <summary>
    /// Cloud cover.
    /// </summary>
    [JsonPropertyName("cloud_cover")]
    public double? CloudCover { get; set; }

    /// <summary>
    /// Mean sea level pressure.
    /// </summary>
    [JsonPropertyName("pressure_msl")]
    public double? Pressure { get; set; }

    /// <summary>
    /// Wind speed at 10m.
    /// </summary>
    [JsonPropertyName("wind_speed_10m")]
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind direction at 10m.
    /// </summary>
    [JsonPropertyName("wind_direction_10m")]
    public double? WindDirection { get; set; }

    /// <summary>
    /// Wind gusts at 10m.
    /// </summary>
    [JsonPropertyName("wind_gusts_10m")]
    public double? WindGusts { get; set; }
}