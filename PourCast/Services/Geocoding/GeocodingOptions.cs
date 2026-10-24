namespace PourCast.Services.Geocoding;

/// <summary>
/// Configuration for the place search.
/// </summary>
public class GeocodingOptions
{
    /// <summary>
    /// The base URL of the Open-Meteo geocoding API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The maximum number of results to return.
    /// </summary>
    public int ResultCount { get; set; } = 8;

    /// <summary>
    /// How long to cache search results for, in minutes.
    /// </summary>
    public int CacheMinutes { get; set; } = 1440;

    /// <summary>
    /// The key calls are counted against, shared with the Open-Meteo forecast providers.
    /// </summary>
    public string UsageKey { get; set; } = string.Empty;

    /// <summary>
    /// The maximum number of calls per UTC day, or null for no limit.
    /// </summary>
    public int? DailyLimit { get; set; }
}