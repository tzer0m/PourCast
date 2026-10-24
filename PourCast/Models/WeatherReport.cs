namespace PourCast.Models;

/// <summary>
/// Forecasts from all sources for a location, plus the combined view.
/// </summary>
public class WeatherReport
{
    /// <summary>
    /// The name of the location, from the first source that supplies one.
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// The result from each source.
    /// </summary>
    public List<ProviderResult> Sources { get; set; } = [];

    /// <summary>
    /// The hourly forecast combined across sources.
    /// </summary>
    public List<HourlySummary> Hourly { get; set; } = [];

    /// <summary>
    /// The combined forecast in the range's chart columns: 6 hours for the week, a day for the month.
    /// </summary>
    public List<HourlySummary> Columns { get; set; } = [];

    /// <summary>
    /// Sunrise and sunset for the days covered by the forecast.
    /// </summary>
    public List<Daylight> Daylight { get; set; } = [];
}