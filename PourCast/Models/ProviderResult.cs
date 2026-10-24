using System.Text.Json.Serialization;

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
    /// How the provider was used for this report.
    /// </summary>
    public ProviderStatus Status { get; set; }

    /// <summary>
    /// The ranges the provider is configured for.
    /// </summary>
    public List<ForecastRange> Ranges { get; set; } = [];

    /// <summary>
    /// The forecast, if the request succeeded. Not sent to the browser, which only needs the combined hours.
    /// </summary>
    [JsonIgnore]
    public Forecast? Forecast { get; set; }

    /// <summary>
    /// The error message, if the request failed.
    /// </summary>
    public string? Error { get; set; }
}