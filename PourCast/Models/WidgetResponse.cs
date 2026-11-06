namespace PourCast.Models;

/// <summary>
/// The weather shown by the Tidgets home screen widget.
/// </summary>
public class WidgetResponse
{
    /// <summary>
    /// The place name.
    /// </summary>
    public string Place { get; set; } = string.Empty;

    /// <summary>
    /// The current temperature in °C.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// The current condition, as the name of a WeatherCondition.
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// Whether the sun is up now.
    /// </summary>
    public bool IsDay { get; set; }

    /// <summary>
    /// The highest temperature in the next 24 hours, in °C.
    /// </summary>
    public double? High { get; set; }

    /// <summary>
    /// The lowest temperature in the next 24 hours, in °C.
    /// </summary>
    public double? Low { get; set; }

    /// <summary>
    /// Whether it is raining in the current hour.
    /// </summary>
    public bool IsRaining { get; set; }

    /// <summary>
    /// When the next rain starts, or null if it's raining now or none is forecast.
    /// </summary>
    public DateTimeOffset? RainStart { get; set; }

    /// <summary>
    /// When the current rain stops, or null if it isn't raining or it lasts beyond the forecast window.
    /// </summary>
    public DateTimeOffset? RainEnd { get; set; }

    /// <summary>
    /// The next few hours.
    /// </summary>
    public List<WidgetHour> Hourly { get; set; } = [];

    /// <summary>
    /// The next few days, starting tomorrow.
    /// </summary>
    public List<WidgetDay> Daily { get; set; } = [];
}