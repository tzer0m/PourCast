namespace PourCast.Models;

/// <summary>
/// One day of the widget's daily forecast.
/// </summary>
public class WidgetDay
{
    /// <summary>
    /// The date, in the place's time zone.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The day's main condition, as the name of a WeatherCondition.
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// The lowest temperature in °C.
    /// </summary>
    public double? Low { get; set; }

    /// <summary>
    /// The highest temperature in °C.
    /// </summary>
    public double? High { get; set; }

    /// <summary>
    /// The highest UV index of the day.
    /// </summary>
    public double? UvIndex { get; set; }
}