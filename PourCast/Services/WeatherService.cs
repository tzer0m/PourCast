using PourCast.Models;
using PourCast.Providers;

namespace PourCast.Services;

/// <summary>
/// Gathers forecasts from every registered weather provider.
/// </summary>
/// <param name="providers">The registered weather providers.</param>
/// <param name="logger">The logger.</param>
public class WeatherService(IEnumerable<IWeatherProvider> providers, ILogger<WeatherService> logger)
{
    /// <summary>
    /// Gets forecasts for a location from all providers in parallel.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<List<ProviderResult>> GetForecastsAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        IEnumerable<Task<ProviderResult>> tasks = providers.Select(x => GetForecastAsync(x, latitude, longitude, cancellationToken));
        ProviderResult[] results = await Task.WhenAll(tasks);
        return [.. results];
    }

    /// <summary>
    /// Gets a forecast from a single provider, capturing any failure.
    /// </summary>
    /// <param name="provider">The weather provider.</param>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<ProviderResult> GetForecastAsync(IWeatherProvider provider, double latitude, double longitude, CancellationToken cancellationToken)
    {
        try
        {
            Forecast forecast = await provider.GetForecastAsync(latitude, longitude, cancellationToken);
            return new ProviderResult { Source = provider.Name, Forecast = forecast };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to get forecast from {Provider}.", provider.Name);
            return new ProviderResult { Source = provider.Name, Error = ex.Message };
        }
    }
}