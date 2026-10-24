namespace PourCast.Scoring.Models;

/// <summary>
/// A forecast value that's scored against observations.
/// </summary>
public enum ScoreMetric
{
    /// <summary>
    /// Temperature, by mean absolute error in °C.
    /// </summary>
    Temperature,

    /// <summary>
    /// Feels like temperature, by mean absolute error in °C.
    /// </summary>
    FeelsLike,

    /// <summary>
    /// Relative humidity, by mean absolute error in %.
    /// </summary>
    Humidity,

    /// <summary>
    /// Wind speed, by mean absolute error in mph.
    /// </summary>
    WindSpeed,

    /// <summary>
    /// Wind gusts, by mean absolute error in mph, for hours a gust was reported.
    /// </summary>
    WindGust,

    /// <summary>
    /// Wind direction, by mean angular error in degrees, for hours with enough wind.
    /// </summary>
    WindDirection,

    /// <summary>
    /// Whether it rained, by Brier score of the chance of rain.
    /// </summary>
    Rain
}