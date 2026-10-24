using PourCast.Models;

namespace PourCast.Providers;

/// <summary>
/// A source of weather forecast data.
/// </summary>
public interface IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The forecast ranges this provider is used for.
    /// </summary>
    IReadOnlyList<ForecastRange> Ranges { get; }

    /// <summary>
    /// The key the provider's calls are counted against.
    /// </summary>
    string UsageKey { get; }

    /// <summary>
    /// The maximum number of calls per UTC day, or null for no limit.
    /// </summary>
    int? DailyLimit { get; }

    /// <summary>
    /// Gets the forecast for a location.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken);
}