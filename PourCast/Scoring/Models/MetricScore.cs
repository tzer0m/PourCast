namespace PourCast.Scoring.Models;

/// <summary>
/// How accurate one source was for one metric at one day ahead.
/// </summary>
public class MetricScore
{
    /// <summary>
    /// The average error in the metric's units, or the Brier score for rain.
    /// </summary>
    public double Error { get; set; }

    /// <summary>
    /// The error as a score from 0 to 100, where 100 is perfect.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// How many hours were compared.
    /// </summary>
    public int Matches { get; set; }
}