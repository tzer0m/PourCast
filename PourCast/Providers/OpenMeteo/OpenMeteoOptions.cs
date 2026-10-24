namespace PourCast.Providers.OpenMeteo;

/// <summary>
/// Configuration for the Open-Meteo provider.
/// </summary>
public class OpenMeteoOptions
{
    /// <summary>
    /// The base URL of the Open-Meteo API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The number of days to forecast.
    /// </summary>
    public int ForecastDays { get; set; } = 7;

    /// <summary>
    /// How long to cache forecasts for, in minutes.
    /// </summary>
    public int CacheMinutes { get; set; } = 15;
}