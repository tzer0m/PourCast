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
    /// Gets the forecast for a location.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken);
}