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
        List<Daylight> daylight = SunCalculator.Calculate(latitude, longitude, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), 9);
        return new WeatherReport { Sources = [.. results], Hourly = hourly, Daylight = daylight };
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
        List<double> feelsLike = hour.Where(x => x.FeelsLike is not null).Select(x => x.FeelsLike!.Value).ToList();
        List<double> uvIndex = hour.Where(x => x.UvIndex is not null).Select(x => x.UvIndex!.Value).ToList();
        List<double> uvIndexClearSky = hour.Where(x => x.UvIndexClearSky is not null).Select(x => x.UvIndexClearSky!.Value).ToList();
        List<double> windSpeed = hour.Where(x => x.WindSpeed is not null).Select(x => x.WindSpeed!.Value).ToList();
        List<double> humidity = hour.Where(x => x.Humidity is not null).Select(x => x.Humidity!.Value).ToList();
        List<double> precipitation = hour.Where(x => x.Precipitation is not null).Select(x => x.Precipitation!.Value).ToList();
        List<double> probabilities = hour.Where(x => x.PrecipitationProbability is not null).Select(x => x.PrecipitationProbability!.Value).ToList();
        return new HourlySummary
        {
            Time = hour.Key,
            SourceCount = hour.Count(),
            Condition = hour.Where(x => x.Condition != WeatherCondition.Unknown).GroupBy(x => x.Condition).OrderByDescending(x => x.Count()).Select(x => x.Key).FirstOrDefault(),
            TemperatureAverage = Average(temperatures),
            TemperatureMin = Min(temperatures),
            TemperatureMax = Max(temperatures),
            FeelsLikeAverage = Average(feelsLike),
            FeelsLikeMin = Min(feelsLike),
            FeelsLikeMax = Max(feelsLike),
            UvIndexAverage = Average(uvIndex),
            UvIndexMin = Min(uvIndex),
            UvIndexMax = Max(uvIndex),
            UvIndexClearSkyAverage = Average(uvIndexClearSky),
            UvIndexClearSkyMin = Min(uvIndexClearSky),
            UvIndexClearSkyMax = Max(uvIndexClearSky),
            WindSpeedAverage = Average(windSpeed),
            HumidityAverage = Average(humidity),
            HumidityMin = Min(humidity),
            HumidityMax = Max(humidity),
            PrecipitationAverage = Average(precipitation),
            PrecipitationMin = Min(precipitation),
            PrecipitationMax = Max(precipitation),
            PrecipitationProbabilityAverage = Average(probabilities)
        };
    }

    /// <summary>
    /// Returns the average of the values, or null if there are none.
    /// </summary>
    /// <param name="values">The values.</param>
    private static double? Average(List<double> values)
    {
        return values.Count > 0 ? values.Average() : null;
    }

    /// <summary>
    /// Returns the lowest of the values, or null if there are none.
    /// </summary>
    /// <param name="values">The values.</param>
    private static double? Min(List<double> values)
    {
        return values.Count > 0 ? values.Min() : null;
    }

    /// <summary>
    /// Returns the highest of the values, or null if there are none.
    /// </summary>
    /// <param name="values">The values.</param>
    private static double? Max(List<double> values)
    {
        return values.Count > 0 ? values.Max() : null;
    }
}