namespace PourCast.Models;

/// <summary>
/// Current weather conditions at a location.
/// </summary>
public class CurrentConditions
{
    /// <summary>
    /// The time the conditions apply to.
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// The normalised weather condition.
    /// </summary>
    public WeatherCondition Condition { get; set; }

    /// <summary>
    /// Whether it is daytime.
    /// </summary>
    public bool? IsDay { get; set; }

    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Apparent (feels like) temperature in °C.
    /// </summary>
    public double? FeelsLike { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    public double? Humidity { get; set; }

    /// <summary>
    /// Precipitation in mm.
    /// </summary>
    public double? Precipitation { get; set; }

    /// <summary>
    /// Cloud cover in percent.
    /// </summary>
    public double? CloudCover { get; set; }

    /// <summary>
    /// Mean sea level pressure in hPa.
    /// </summary>
    public double? Pressure { get; set; }

    /// <summary>
    /// Wind speed in mph.
    /// </summary>
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind gust speed in mph.
    /// </summary>
    public double? WindGust { get; set; }

    /// <summary>
    /// Wind direction in degrees.
    /// </summary>
    public double? WindDirection { get; set; }
}