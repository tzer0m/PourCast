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

    /// <summary>
    /// The key calls are counted against. Providers on the same API account share a key so they share the limit.
    /// </summary>
    public string UsageKey { get; set; } = string.Empty;

    /// <summary>
    /// The maximum number of calls per UTC day, or null for no limit.
    /// </summary>
    public int? DailyLimit { get; set; }
}