using PourCast.Scoring.Models;

namespace PourCast.Scoring;

/// <summary>
/// Configuration for recording and scoring forecasts.
/// </summary>
public class ScoringOptions
{
    /// <summary>
    /// The name of the place forecasts are recorded for.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The latitude forecasts are recorded for, the METAR station's own.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude forecasts are recorded for, the METAR station's own.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The ICAO code of the METAR station observations come from.
    /// </summary>
    public string Station { get; set; } = string.Empty;

    /// <summary>
    /// The base URL of the aviationweather.gov data API.
    /// </summary>
    public string MetarBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// How many hours of METARs to fetch each time, so gaps from restarts are filled in.
    /// </summary>
    public int MetarHours { get; set; } = 24;

    /// <summary>
    /// The UTC hours forecasts are recorded at.
    /// </summary>
    public List<int> SnapshotHours { get; set; } = [];

    /// <summary>
    /// How many days ahead of each snapshot to record.
    /// </summary>
    public int SnapshotDays { get; set; } = 7;

    /// <summary>
    /// How long to wait before retrying a snapshot that failed, in minutes.
    /// </summary>
    public int RetryMinutes { get; set; } = 10;

    /// <summary>
    /// The source name the combined forecast is recorded under.
    /// </summary>
    public string CombinedSource { get; set; } = string.Empty;

    /// <summary>
    /// How many days of predictions are scored.
    /// </summary>
    public int WindowDays { get; set; } = 30;

    /// <summary>
    /// The fewest hours needed before a metric is scored for a day ahead.
    /// </summary>
    public int MinimumMatches { get; set; } = 24;

    /// <summary>
    /// The error at which each metric scores zero, e.g. 5°C for temperature, or a Brier score of 0.25 for rain.
    /// </summary>
    public Dictionary<ScoreMetric, double> Tolerances { get; set; } = [];

    /// <summary>
    /// The predicted rain amount in mm that counts as rain, for sources without a chance of rain.
    /// </summary>
    public double RainThreshold { get; set; } = 0.1;

    /// <summary>
    /// The observed wind speed in mph below which wind direction isn't scored, as it's unreliable in light winds.
    /// </summary>
    public double DirectionMinimumWind { get; set; } = 5;

    /// <summary>
    /// How long worked out scores are cached for, in minutes.
    /// </summary>
    public int ScoreCacheMinutes { get; set; } = 30;
}