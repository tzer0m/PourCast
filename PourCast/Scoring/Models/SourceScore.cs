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
    /// The average of the day scores, from 0 to 100, or null while there isn't enough data.
    /// </summary>
    public double? Overall { get; set; }

    /// <summary>
    /// The scores for each day ahead that had enough hours.
    /// </summary>
    public List<DayScore> Days { get; set; } = [];
}