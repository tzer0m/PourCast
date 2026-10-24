namespace PourCast.Models;

/// <summary>
/// Combined forecast for a single hour across all sources.
/// </summary>
public class HourlySummary
{
    /// <summary>
    /// The start of the hour in UTC.
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// The number of sources with data for this hour.
    /// </summary>
    public int SourceCount { get; set; }

    /// <summary>
    /// Average temperature across sources in °C.
    /// </summary>
    public double? TemperatureAverage { get; set; }

    /// <summary>
    /// Lowest temperature across sources in °C.
    /// </summary>
    public double? TemperatureMin { get; set; }

    /// <summary>
    /// Highest temperature across sources in °C.
    /// </summary>
    public double? TemperatureMax { get; set; }

    /// <summary>
    /// Average feels like temperature across sources in °C.
    /// </summary>
    public double? FeelsLikeAverage { get; set; }

    /// <summary>
    /// Average precipitation across sources in mm.
    /// </summary>
    public double? PrecipitationAverage { get; set; }

    /// <summary>
    /// Average chance of precipitation across sources in percent.
    /// </summary>
    public double? PrecipitationProbabilityAverage { get; set; }
}