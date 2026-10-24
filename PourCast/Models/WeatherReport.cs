namespace PourCast.Models;

/// <summary>
/// Forecasts from all sources for a location, plus the combined view.
/// </summary>
public class WeatherReport
{
    /// <summary>
    /// The result from each source.
    /// </summary>
    public List<ProviderResult> Sources { get; set; } = [];

    /// <summary>
    /// The hourly forecast combined across sources.
    /// </summary>
    public List<HourlySummary> Hourly { get; set; } = [];
}