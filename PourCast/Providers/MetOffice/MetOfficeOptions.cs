namespace PourCast.Providers.MetOffice;

/// <summary>
/// Configuration for the Met Office DataHub provider.
/// </summary>
public class MetOfficeOptions
{
    /// <summary>
    /// The base URL of the site-specific API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The DataHub API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// How long to cache forecasts for, in minutes.
    /// </summary>
    public int CacheMinutes { get; set; } = 15;
}