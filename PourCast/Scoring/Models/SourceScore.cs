namespace PourCast.Scoring.Models;

/// <summary>
/// How accurate one source has been, overall and by day ahead.
/// </summary>
public class SourceScore
{
    /// <summary>
    /// The forecast source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The score from 0 to 100, compared with the other sources on the same days and metrics, or null while there isn't enough data.
    /// </summary>
    public double? Overall { get; set; }

    /// <summary>
    /// How many points above or below the average of the other sources it was, on the same days and metrics.
    /// </summary>
    public double? Skill { get; set; }

    /// <summary>
    /// The average of the day scores over the days every source covers, or null if there are none.
    /// </summary>
    public double? CommonOverall { get; set; }

    /// <summary>
    /// The scores for each day ahead that had enough hours.
    /// </summary>
    public List<DayScore> Days { get; set; } = [];
}