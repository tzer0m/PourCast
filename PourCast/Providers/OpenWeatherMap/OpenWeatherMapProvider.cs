using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.OpenWeatherMap.Models;
using System.Globalization;

namespace PourCast.Providers.OpenWeatherMap;

/// <summary>
/// Weather provider backed by the free OpenWeatherMap 5 day / 3 hour forecast API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for OpenWeatherMap.</param>
/// <param name="options">The OpenWeatherMap options.</param>
/// <param name="cache">The memory cache.</param>
public class OpenWeatherMapProvider(HttpClient httpClient, IOptions<OpenWeatherMapOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "OpenWeatherMap";

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
    /// Gets the forecast for a location from OpenWeatherMap, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"openweathermap:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async (ICacheEntry entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the 3 hourly forecast from OpenWeatherMap and spreads it into hours.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("The OpenWeatherMap API key is not configured.");
        }
        string url = string.Create(CultureInfo.InvariantCulture, $"data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&appid={options.Value.ApiKey}");
        OpenWeatherMapResponse response = await httpClient.GetFromJsonAsync<OpenWeatherMapResponse>(url, cancellationToken) ?? throw new InvalidOperationException("OpenWeatherMap returned an empty response.");
        return new Forecast
        {
            Source = Name,
            Latitude = latitude,
            Longitude = longitude,
            RetrievedAt = DateTimeOffset.UtcNow,
            Hourly = SpreadIntoHours(response.Steps)
        };
    }

    /// <summary>
    /// Turns 3 hourly steps into hours. Readings like temperature and wind are taken at the step's own hour,
    /// while rain, its chance and the condition cover the 3 hours before the step, so they're shared across those hours.
    /// </summary>
    /// <param name="steps">The 3 hourly steps.</param>
    private static List<HourlyForecast> SpreadIntoHours(List<OpenWeatherMapStep> steps)
    {
        Dictionary<DateTimeOffset, HourlyForecast> hours = [];
        HourlyForecast HourAt(DateTimeOffset time)
        {
            if (!hours.TryGetValue(time, out HourlyForecast? hour))
            {
                hour = new HourlyForecast { Time = time };
                hours[time] = hour;
            }
            return hour;
        }
        foreach (OpenWeatherMapStep step in steps)
        {
            DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(step.Dt);
            HourlyForecast reading = HourAt(time);
            reading.Temperature = step.Main?.Temp;
            reading.FeelsLike = step.Main?.FeelsLike;
            reading.Humidity = step.Main?.Humidity;
            reading.CloudCover = step.Clouds?.All;
            reading.WindSpeed = ToMph(step.Wind?.Speed);
            reading.WindGust = ToMph(step.Wind?.Gust);
            reading.WindDirection = step.Wind?.Deg;

            // Rain and snow totals cover the previous 3 hours; a missing total means none fell.
            double total = (step.Rain?.ThreeHours ?? 0) + (step.Snow?.ThreeHours ?? 0);
            WeatherCondition condition = MapCondition(step.Weather.FirstOrDefault()?.Id);
            for (int offset = 1; offset <= 3; offset++)
            {
                HourlyForecast hour = HourAt(time.AddHours(-offset));
                hour.Precipitation = total / 3;
                hour.PrecipitationProbability = step.Pop * 100;
                hour.Condition = condition;
            }
        }
        return hours.Values.OrderBy(x => x.Time).ToList();
    }

    /// <summary>
    /// Converts a speed in m/s to mph.
    /// </summary>
    /// <param name="metresPerSecond">The speed in m/s.</param>
    private static double? ToMph(double? metresPerSecond)
    {
        return metresPerSecond * 2.236936;
    }

    /// <summary>
    /// Maps an OpenWeatherMap condition ID to a normalised condition.
    /// </summary>
    /// <param name="id">The condition ID.</param>
    private static WeatherCondition MapCondition(int? id)
    {
        return id switch
        {
            >= 200 and < 300 => WeatherCondition.Thunderstorm,
            >= 300 and < 400 => WeatherCondition.Drizzle,
            500 or 501 => WeatherCondition.Rain,
            >= 502 and <= 504 => WeatherCondition.HeavyRain,
            511 => WeatherCondition.FreezingRain,
            >= 520 and < 600 => WeatherCondition.Showers,
            >= 611 and <= 616 => WeatherCondition.Sleet,
            >= 620 and < 700 => WeatherCondition.SnowShowers,
            >= 600 and < 700 => WeatherCondition.Snow,
            >= 700 and < 800 => WeatherCondition.Fog,
            800 => WeatherCondition.Clear,
            801 or 802 => WeatherCondition.PartlyCloudy,
            803 or 804 => WeatherCondition.Cloudy,
            _ => WeatherCondition.Unknown
        };
    }
}