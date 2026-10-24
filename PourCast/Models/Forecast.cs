namespace PourCast.Models;

/// <summary>
/// A full forecast for a location from a single provider.
/// </summary>
public class Forecast
{
    /// <summary>
    /// The name of the provider the forecast came from.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The name of the location, if the provider supplies one.
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// The latitude of the location.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude of the location.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The time the forecast was retrieved.
    /// </summary>
    public DateTimeOffset RetrievedAt { get; set; }

    /// <summary>
    /// The current conditions, if the provider supplies them.
    /// </summary>
    public CurrentConditions? Current { get; set; }

    /// <summary>
    /// The hourly forecast.
    /// </summary>
    public List<HourlyForecast> Hourly { get; set; } = [];

    /// <summary>
    /// The daily forecast.
    /// </summary>
    public List<DailyForecast> Daily { get; set; } = [];
}