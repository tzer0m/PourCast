using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// Instant values from MET Norway.
/// </summary>
public class MetNorwayInstantDetails
{
    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    [JsonPropertyName("air_temperature")]
    public double? AirTemperature { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    [JsonPropertyName("relative_humidity")]
    public double? RelativeHumidity { get; set; }

    /// <summary>
    /// Wind speed in m/s.
    /// </summary>
    [JsonPropertyName("wind_speed")]
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind gust speed in m/s.
    /// </summary>
    [JsonPropertyName("wind_speed_of_gust")]
    public double? WindSpeedOfGust { get; set; }

    /// <summary>
    /// Direction the wind is coming from in degrees.
    /// </summary>
    [JsonPropertyName("wind_from_direction")]
    public double? WindFromDirection { get; set; }

    /// <summary>
    /// Cloud cover in percent.
    /// </summary>
    [JsonPropertyName("cloud_area_fraction")]
    public double? CloudAreaFraction { get; set; }

    /// <summary>
    /// UV index with a clear sky.
    /// </summary>
    [JsonPropertyName("ultraviolet_index_clear_sky")]
    public double? UltravioletIndexClearSky { get; set; }
}