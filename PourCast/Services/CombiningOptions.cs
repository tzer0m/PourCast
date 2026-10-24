namespace PourCast.Services;

/// <summary>
/// Configuration for how sources are combined into the line and band.
/// </summary>
public class CombiningOptions
{
    /// <summary>
    /// The number of sources with a value needed before the band is trimmed to percentiles instead of the full min to max.
    /// </summary>
    public int PercentileMinimumSources { get; set; } = 6;

    /// <summary>
    /// The lower edge of a trimmed band, as a fraction, e.g. 0.1 for the 10th percentile.
    /// </summary>
    public double LowPercentile { get; set; } = 0.1;

    /// <summary>
    /// The upper edge of a trimmed band, as a fraction, e.g. 0.9 for the 90th percentile.
    /// </summary>
    public double HighPercentile { get; set; } = 0.9;

    /// <summary>
    /// The lower edge of an ensemble's band, as a fraction.
    /// </summary>
    public double EnsembleLowPercentile { get; set; } = 0.1;

    /// <summary>
    /// The upper edge of an ensemble's band, as a fraction.
    /// </summary>
    public double EnsembleHighPercentile { get; set; } = 0.9;

    /// <summary>
    /// Whether to calculate feels like ourselves for every source instead of using each provider's own value.
    /// </summary>
    public bool CalculateFeelsLike { get; set; } = true;

    /// <summary>
    /// The temperature in °C at or below which wind chill applies in full.
    /// </summary>
    public double WindChillFullAt { get; set; } = 15;

    /// <summary>
    /// The temperature in °C by which wind chill has faded out completely.
    /// </summary>
    public double WindChillFadeTo { get; set; } = 20;

    /// <summary>
    /// The temperature in °C above which the heat index starts to fade in.
    /// </summary>
    public double HeatIndexFadeFrom { get; set; } = 24;

    /// <summary>
    /// The temperature in °C at or above which the heat index applies in full.
    /// </summary>
    public double HeatIndexFullAt { get; set; } = 27;
}