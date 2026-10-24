using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.VisualCrossing.Models;
using System.Globalization;

namespace PourCast.Providers.VisualCrossing;

/// <summary>
/// Weather provider backed by the Visual Crossing timeline API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for Visual Crossing.</param>
/// <param name="options">The Visual Crossing options.</param>
/// <param name="cache">The memory cache.</param>
public class VisualCrossingProvider(HttpClient httpClient, IOptions<VisualCrossingOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "Visual Crossing";

    /// <summary>
    /// The forecast ranges this provider is used for.
    /// </summary>
    public IReadOnlyList<ForecastRange> Ranges => options.Value.Ranges;

    /// <summary>
    /// The key the provider's calls are counted against.
    /// </summary>
    public string UsageKey => options.Value.UsageKey;

    /// <summary>
    /// The maximum number of calls per UTC day, or null for no limit.
    /// </summary>
    public int? DailyLimit => options.Value.DailyLimit;

    /// <summary>
    /// Gets the forecast for a location from Visual Crossing, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"visualcrossing:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async (ICacheEntry entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the hourly forecast from Visual Crossing and maps it.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("The Visual Crossing API key is not configured.");
        }

        // A fixed date range keeps the record count (one per hour) down; UK units give °C, mph and mm.
        string start = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string end = DateTime.UtcNow.AddDays(options.Value.ForecastDays - 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string url = string.Create(CultureInfo.InvariantCulture, $"VisualCrossingWebServices/rest/services/timeline/{latitude},{longitude}/{start}/{end}?unitGroup=uk&include=hours&contentType=json&key={options.Value.ApiKey}");
        VisualCrossingResponse response = await httpClient.GetFromJsonAsync<VisualCrossingResponse>(url, cancellationToken) ?? throw new InvalidOperationException("Visual Crossing returned an empty response.");
        return new Forecast
        {
            Source = Name,
            Latitude = response.Latitude,
            Longitude = response.Longitude,
            RetrievedAt = DateTimeOffset.UtcNow,
            Hourly = response.Days.SelectMany(x => x.Hours).Select(MapHour).ToList()
        };
    }

    /// <summary>
    /// Maps a Visual Crossing hour to an hourly forecast.
    /// </summary>
    /// <param name="hour">The hour.</param>
    private static HourlyForecast MapHour(VisualCrossingHour hour)
    {
        return new HourlyForecast
        {
            Time = DateTimeOffset.FromUnixTimeSeconds(hour.DatetimeEpoch),
            Condition = MapCondition(hour.Icon),
            Temperature = hour.Temp,
            FeelsLike = hour.FeelsLike,
            Humidity = hour.Humidity,
            Precipitation = hour.Precip,
            PrecipitationProbability = hour.PrecipProb,
            WindSpeed = hour.WindSpeed,
            WindGust = hour.WindGust,
            WindDirection = hour.WindDir,
            CloudCover = hour.CloudCover,
            UvIndex = hour.UvIndex
        };
    }

    /// <summary>
    /// Maps a Visual Crossing icon name to a normalised condition.
    /// </summary>
    /// <param name="icon">The icon name.</param>
    private static WeatherCondition MapCondition(string? icon)
    {
        return icon switch
        {
            "clear-day" or "clear-night" => WeatherCondition.Clear,
            "partly-cloudy-day" or "partly-cloudy-night" => WeatherCondition.PartlyCloudy,
            "cloudy" or "wind" => WeatherCondition.Cloudy,
            "fog" => WeatherCondition.Fog,
            "rain" => WeatherCondition.Rain,
            "showers-day" or "showers-night" => WeatherCondition.Showers,
            "snow" => WeatherCondition.Snow,
            "snow-showers-day" or "snow-showers-night" => WeatherCondition.SnowShowers,
            "sleet" => WeatherCondition.Sleet,
            "hail" => WeatherCondition.Hail,
            "thunder" or "thunder-rain" or "thunder-showers-day" or "thunder-showers-night" => WeatherCondition.Thunderstorm,
            _ => WeatherCondition.Unknown
        };
    }
}