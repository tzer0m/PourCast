using PourCast.Models;
using PourCast.Providers;

namespace PourCast.Services;

/// <summary>
/// Gathers forecasts from every registered weather provider and combines them.
/// </summary>
/// <param name="providers">The registered weather providers.</param>
/// <param name="logger">The logger.</param>
public class WeatherService(IEnumerable<IWeatherProvider> providers, ILogger<WeatherService> logger)
{
    /// <summary>
    /// Gets forecasts for a location from all providers in parallel and combines them.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<WeatherReport> GetReportAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        IEnumerable<Task<ProviderResult>> tasks = providers.Select(x => GetForecastAsync(x, latitude, longitude, cancellationToken));
        ProviderResult[] results = await Task.WhenAll(tasks);
        List<HourlySummary> hourly = results.Where(x => x.Forecast is not null).SelectMany(x => x.Forecast!.Hourly).GroupBy(x => x.Time.ToUniversalTime()).OrderBy(x => x.Key).Select(Summarise).ToList();
        return new WeatherReport { Sources = [.. results], Hourly = hourly };
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

    /// <summary>
    /// Combines every source's forecast for one hour.
    /// </summary>
    /// <param name="hour">The forecasts for the hour, grouped by UTC time.</param>
    private static HourlySummary Summarise(IGrouping<DateTimeOffset, HourlyForecast> hour)
    {
        List<double> temperatures = hour.Where(x => x.Temperature is not null).Select(x => x.Temperature!.Value).ToList();
        List<double> precipitation = hour.Where(x => x.Precipitation is not null).Select(x => x.Precipitation!.Value).ToList();
        List<double> probabilities = hour.Where(x => x.PrecipitationProbability is not null).Select(x => x.PrecipitationProbability!.Value).ToList();
        return new HourlySummary
        {
            Time = hour.Key,
            SourceCount = hour.Count(),
            TemperatureAverage = temperatures.Count > 0 ? temperatures.Average() : null,
            TemperatureMin = temperatures.Count > 0 ? temperatures.Min() : null,
            TemperatureMax = temperatures.Count > 0 ? temperatures.Max() : null,
            PrecipitationAverage = precipitation.Count > 0 ? precipitation.Average() : null,
            PrecipitationProbabilityAverage = probabilities.Count > 0 ? probabilities.Average() : null
        };
    }
}