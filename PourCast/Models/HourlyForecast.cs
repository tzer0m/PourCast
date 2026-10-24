namespace PourCast.Models;

/// <summary>
/// Forecast for a single hour.
/// </summary>
public class HourlyForecast
{
    /// <summary>
    /// The start of the hour.
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// The ensemble member this hour belongs to, or null for a normal forecast.
    /// </summary>
    public string? Member { get; set; }

    /// <summary>
    /// The normalised weather condition.
    /// </summary>
    public WeatherCondition Condition { get; set; }

    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Apparent (feels like) temperature in °C.
    /// </summary>
    public double? FeelsLike { get; set; }

    /// <summary>
    /// Chance of precipitation in percent.
    /// </summary>
    public double? PrecipitationProbability { get; set; }

    /// <summary>
    /// Precipitation in mm.
    /// </summary>
    public double? Precipitation { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    public double? Humidity { get; set; }

    /// <summary>
    /// Cloud cover in percent.
    /// </summary>
    public double? CloudCover { get; set; }

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

    /// <summary>
    /// UV index, allowing for cloud cover.
    /// </summary>
    public double? UvIndex { get; set; }

    /// <summary>
    /// UV index with a clear sky.
    /// </summary>
    public double? UvIndexClearSky { get; set; }

    /// <summary>
    /// Returns a shallow copy, so values can be adjusted without changing a cached forecast.
    /// </summary>
    public HourlyForecast Copy()
    {
        return (HourlyForecast)MemberwiseClone();
    }
}