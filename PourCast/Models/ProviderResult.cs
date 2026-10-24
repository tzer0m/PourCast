namespace PourCast.Models;

/// <summary>
/// The outcome of requesting a forecast from a single provider.
/// </summary>
public class ProviderResult
{
    /// <summary>
    /// The name of the provider.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The forecast, if the request succeeded.
    /// </summary>
    public Forecast? Forecast { get; set; }

    /// <summary>
    /// The error message, if the request failed.
    /// </summary>
    public string? Error { get; set; }
}