namespace PourCast.Scoring.Models;

/// <summary>
/// Every source's scores, with the days they can all be compared on.
/// </summary>
public class ScoreBoard
{
    /// <summary>
    /// Every source's scores, best first.
    /// </summary>
    public List<SourceScore> Sources { get; set; } = [];

    /// <summary>
    /// How many days ahead, from day 1, every scored source covers.
    /// </summary>
    public int CommonDays { get; set; }
}