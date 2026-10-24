using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers;
using PourCast.Services.Usage;
using System.Globalization;

namespace PourCast.Services;

/// <summary>
/// Gathers forecasts from every registered weather provider and combines them.
/// </summary>
/// <param name="providers">The registered weather providers.</param>
/// <param name="usage">The API usage tracker.</param>
/// <param name="usageOptions">The usage options, for how long to remember failures.</param>
/// <param name="combiningOptions">How sources are combined into the line and band.</param>
/// <param name="cache">The memory cache, used to remember recent failures.</param>
/// <param name="logger">The logger.</param>
public class WeatherService(IEnumerable<IWeatherProvider> providers, UsageTracker usage, IOptions<UsageOptions> usageOptions, IOptions<CombiningOptions> combiningOptions, IMemoryCache cache, ILogger<WeatherService> logger)
{
    /// <summary>
    /// Gets forecasts for a location from all providers in parallel and combines them.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="range">The forecast range, which decides which providers are used and the chart columns.</param>
    /// <param name="timeZone">The viewer's time zone, so columns line up with their clock.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<WeatherReport> GetReportAsync(double latitude, double longitude, ForecastRange range, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        ProviderResult[] results = await GatherAsync(latitude, longitude, range, cancellationToken);
        List<Forecast> forecasts = results.Where(x => x.Forecast is not null).Select(x => x.Forecast!).ToList();
        List<HourlySummary> hourly = ForecastCombiner.Combine(forecasts, ColumnBuilder.Hourly(forecasts), combiningOptions.Value);
        List<HourlySummary> columns = ForecastCombiner.Combine(forecasts, ColumnBuilder.ForRange(range, timeZone, DateTimeOffset.UtcNow), combiningOptions.Value);
        List<Daylight> daylight = SunCalculator.Calculate(latitude, longitude, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), range == ForecastRange.Month ? 38 : 10);
        string? locationName = results.Select(x => x.Forecast?.LocationName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return new WeatherReport { LocationName = locationName, Sources = [.. results], Hourly = hourly, Columns = columns, Daylight = daylight };
    }

    /// <summary>
    /// Asks every provider configured for the range, in parallel, and records today's usage on each result.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="range">The forecast range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<ProviderResult[]> GatherAsync(double latitude, double longitude, ForecastRange range, CancellationToken cancellationToken)
    {
        // Only providers configured for the range are asked; the rest are still listed so the page can show them.
        IEnumerable<Task<ProviderResult>> tasks = providers.Select(x => x.Ranges.Contains(range) ? GetForecastAsync(x, latitude, longitude, cancellationToken) : Task.FromResult(new ProviderResult { Source = x.Name, Status = ProviderStatus.NotUsed, Ranges = [.. x.Ranges] }));
        ProviderResult[] results = await Task.WhenAll(tasks);

        // Usage is read after the calls, so it includes any made for this report.
        foreach ((ProviderResult result, IWeatherProvider provider) in results.Zip(providers))
        {
            result.CallsToday = usage.GetCount(provider.UsageKey);
            result.DailyLimit = provider.DailyLimit;
        }
        return results;
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
        // A provider that failed for this place recently is skipped until the failure expires, rather than called on every page load.
        string failureKey = string.Create(CultureInfo.InvariantCulture, $"failure:{provider.Name}:{latitude:F3}:{longitude:F3}");
        if (cache.TryGetValue(failureKey, out string? recentError))
        {
            return new ProviderResult { Source = provider.Name, Status = ProviderStatus.Failed, Ranges = [.. provider.Ranges], Error = $"{recentError} (retrying shortly)" };
        }
        try
        {
            Forecast forecast = await provider.GetForecastAsync(latitude, longitude, cancellationToken);
            return new ProviderResult { Source = provider.Name, Status = ProviderStatus.Used, Ranges = [.. provider.Ranges], Forecast = forecast };
        }
        catch (DailyLimitExceededException ex)
        {
            logger.LogWarning("Skipped {Provider}: {Message}", provider.Name, ex.Message);
            return new ProviderResult { Source = provider.Name, Status = ProviderStatus.LimitReached, Ranges = [.. provider.Ranges], Error = ex.Message };
        }
        catch (RateLimitedException ex)
        {
            logger.LogWarning("Skipped {Provider}: {Message}", provider.Name, ex.Message);
            return new ProviderResult { Source = provider.Name, Status = ProviderStatus.RateLimited, Ranges = [.. provider.Ranges], Error = ex.Message };
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Includes timeouts, which surface as cancellations even though the page request wasn't cancelled.
            logger.LogWarning(ex, "Failed to get forecast from {Provider}.", provider.Name);
            cache.Set(failureKey, ex.Message, TimeSpan.FromMinutes(usageOptions.Value.FailureCacheMinutes));
            return new ProviderResult { Source = provider.Name, Status = ProviderStatus.Failed, Ranges = [.. provider.Ranges], Error = ex.Message };
        }
    }
}