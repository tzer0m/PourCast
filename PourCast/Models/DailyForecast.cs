namespace PourCast.Models;

/// <summary>
/// Forecast for a single day.
/// </summary>
public class DailyForecast
{
    /// <summary>
    /// The date the forecast applies to.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The normalised weather condition.
    /// </summary>
    public WeatherCondition Condition { get; set; }

    /// <summary>
    /// Maximum temperature in °C.
    /// </summary>
    public double? TemperatureMax { get; set; }

    /// <summary>
    /// Minimum temperature in °C.
    /// </summary>
    public double? TemperatureMin { get; set; }

    /// <summary>
    /// Maximum chance of precipitation in percent.
    /// </summary>
    public double? PrecipitationProbability { get; set; }

    /// <summary>
    /// Total precipitation in mm.
    /// </summary>
    public double? PrecipitationTotal { get; set; }

    /// <summary>
    /// Maximum wind speed in mph.
    /// </summary>
    public double? WindSpeedMax { get; set; }

    /// <summary>
    /// Maximum wind gust speed in mph.
    /// </summary>
    public double? WindGustMax { get; set; }

    /// <summary>
    /// Maximum UV index.
    /// </summary>
    public double? UvIndexMax { get; set; }

    /// <summary>
    /// Time of sunrise.
    /// </summary>
    public DateTimeOffset? Sunrise { get; set; }

    /// <summary>
    /// Time of sunset.
    /// </summary>
    public DateTimeOffset? Sunset { get; set; }
}