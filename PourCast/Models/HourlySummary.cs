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
    /// The condition most sources agree on.
    /// </summary>
    public WeatherCondition Condition { get; set; }

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
    /// Lowest feels like temperature across sources in °C.
    /// </summary>
    public double? FeelsLikeMin { get; set; }

    /// <summary>
    /// Highest feels like temperature across sources in °C.
    /// </summary>
    public double? FeelsLikeMax { get; set; }

    /// <summary>
    /// Average wind speed across sources in mph.
    /// </summary>
    public double? WindSpeedAverage { get; set; }

    /// <summary>
    /// Average relative humidity across sources in percent.
    /// </summary>
    public double? HumidityAverage { get; set; }

    /// <summary>
    /// Lowest relative humidity across sources in percent.
    /// </summary>
    public double? HumidityMin { get; set; }

    /// <summary>
    /// Highest relative humidity across sources in percent.
    /// </summary>
    public double? HumidityMax { get; set; }

    /// <summary>
    /// Average UV index across sources.
    /// </summary>
    public double? UvIndexAverage { get; set; }

    /// <summary>
    /// Lowest UV index across sources.
    /// </summary>
    public double? UvIndexMin { get; set; }

    /// <summary>
    /// Highest UV index across sources.
    /// </summary>
    public double? UvIndexMax { get; set; }

    /// <summary>
    /// Average clear sky UV index across sources.
    /// </summary>
    public double? UvIndexClearSkyAverage { get; set; }

    /// <summary>
    /// Lowest clear sky UV index across sources.
    /// </summary>
    public double? UvIndexClearSkyMin { get; set; }

    /// <summary>
    /// Highest clear sky UV index across sources.
    /// </summary>
    public double? UvIndexClearSkyMax { get; set; }

    /// <summary>
    /// Average precipitation across sources in mm.
    /// </summary>
    public double? PrecipitationAverage { get; set; }

    /// <summary>
    /// Lowest precipitation across sources in mm.
    /// </summary>
    public double? PrecipitationMin { get; set; }

    /// <summary>
    /// Highest precipitation across sources in mm.
    /// </summary>
    public double? PrecipitationMax { get; set; }

    /// <summary>
    /// Average chance of precipitation across sources in percent.
    /// </summary>
    public double? PrecipitationProbabilityAverage { get; set; }
}