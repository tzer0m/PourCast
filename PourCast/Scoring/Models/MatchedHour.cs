namespace PourCast.Scoring.Models;

/// <summary>
/// A predicted hour paired with what was observed for it.
/// </summary>
public class MatchedHour
{
    /// <summary>
    /// The forecast source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Hours between the snapshot and the predicted hour.
    /// </summary>
    public int LeadHours { get; set; }

    /// <summary>
    /// Predicted temperature in °C.
    /// </summary>
    public double? PredictedTemperature { get; set; }

    /// <summary>
    /// Predicted feels like temperature in °C.
    /// </summary>
    public double? PredictedFeelsLike { get; set; }

    /// <summary>
    /// Predicted relative humidity in %.
    /// </summary>
    public double? PredictedHumidity { get; set; }

    /// <summary>
    /// Predicted wind speed in mph.
    /// </summary>
    public double? PredictedWindSpeed { get; set; }

    /// <summary>
    /// Predicted wind gust speed in mph.
    /// </summary>
    public double? PredictedWindGust { get; set; }

    /// <summary>
    /// Predicted wind direction in degrees.
    /// </summary>
    public double? PredictedWindDirection { get; set; }

    /// <summary>
    /// Predicted rain amount in mm.
    /// </summary>
    public double? PredictedPrecipitation { get; set; }

    /// <summary>
    /// Predicted chance of rain in %.
    /// </summary>
    public double? PredictedPrecipitationProbability { get; set; }

    /// <summary>
    /// Observed temperature in °C.
    /// </summary>
    public double? ObservedTemperature { get; set; }

    /// <summary>
    /// Observed feels like temperature in °C.
    /// </summary>
    public double? ObservedFeelsLike { get; set; }

    /// <summary>
    /// Observed relative humidity in %.
    /// </summary>
    public double? ObservedHumidity { get; set; }

    /// <summary>
    /// Observed wind speed in mph.
    /// </summary>
    public double? ObservedWindSpeed { get; set; }

    /// <summary>
    /// Observed wind gust speed in mph, if a gust was reported.
    /// </summary>
    public double? ObservedWindGust { get; set; }

    /// <summary>
    /// Observed wind direction in degrees.
    /// </summary>
    public double? ObservedWindDirection { get; set; }

    /// <summary>
    /// Whether it rained during the hour.
    /// </summary>
    public bool ObservedRaining { get; set; }
}