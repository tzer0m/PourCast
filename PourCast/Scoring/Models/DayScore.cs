namespace PourCast.Scoring.Models;

/// <summary>
/// How accurate one source was at one day ahead.
/// </summary>
public class DayScore
{
    /// <summary>
    /// How many days ahead, from 1 for the first 24 hours after a snapshot.
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// The average of the metric scores, from 0 to 100.
    /// </summary>
    public double Overall { get; set; }

    /// <summary>
    /// How many predicted hours were compared.
    /// </summary>
    public int Matches { get; set; }

    /// <summary>
    /// The percentage of hours where rain or no rain was predicted correctly, or null if there were none.
    /// </summary>
    public double? RainHitRate { get; set; }

    /// <summary>
    /// The score for each metric that had enough hours.
    /// </summary>
    public Dictionary<ScoreMetric, MetricScore> Metrics { get; set; } = [];
}