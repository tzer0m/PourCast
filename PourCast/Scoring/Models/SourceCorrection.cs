namespace PourCast.Scoring.Models;

/// <summary>
/// How one source has done at one day ahead for one metric, used to correct and weight it in the weighted forecast.
/// </summary>
public class SourceCorrection
{
    /// <summary>
    /// The average amount the source has been too high, or too low if negative, to take off its predictions.
    /// </summary>
    public double Bias { get; set; }

    /// <summary>
    /// How much to trust the source, as one over its mean squared error once corrected.
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// How many hours the correction is based on.
    /// </summary>
    public int Matches { get; set; }
}