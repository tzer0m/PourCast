namespace PourCast.Models;

/// <summary>
/// One hour of the widget's hourly strip.
/// </summary>
public class WidgetHour
{
    /// <summary>
    /// The start of the hour.
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// The combined temperature in °C.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// The weather condition, as the name of a WeatherCondition.
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// Whether the sun is up, so the client can pick a day or night icon.
    /// </summary>
    public bool IsDay { get; set; }
}