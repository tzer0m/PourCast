using PourCast.Models;

namespace PourCast.Providers;

/// <summary>
/// Configuration shared by every weather provider.
/// </summary>
public class ProviderOptions
{
    /// <summary>
    /// The base URL of the provider's API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// How long to cache forecasts for, in minutes.
    /// </summary>
    public int CacheMinutes { get; set; } = 15;

    /// <summary>
    /// The forecast ranges this provider is used for.
    /// </summary>
    public List<ForecastRange> Ranges { get; set; } = [];
}