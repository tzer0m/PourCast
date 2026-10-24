namespace PourCast.Models;

/// <summary>
/// One source's mapped hourly forecast, before combining. Used by the source comparison page.
/// </summary>
public class SourceForecast
{
    /// <summary>
    /// The name of the source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// How the source was used.
    /// </summary>
    public ProviderStatus Status { get; set; }

    /// <summary>
    /// The error message, if the source failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The source's hours, as mapped into the common model.
    /// </summary>
    public List<HourlyForecast> Hourly { get; set; } = [];
}