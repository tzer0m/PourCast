using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.TomorrowIo.Models;
using System.Globalization;

namespace PourCast.Providers.TomorrowIo;

/// <summary>
/// Weather provider backed by the Tomorrow.io forecast API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for Tomorrow.io.</param>
/// <param name="options">The Tomorrow.io options.</param>
/// <param name="cache">The memory cache.</param>
public class TomorrowIoProvider(HttpClient httpClient, IOptions<TomorrowIoOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "Tomorrow.io";

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
    /// Gets the forecast for a location from Tomorrow.io, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"tomorrowio:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the hourly forecast from Tomorrow.io and maps it.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("The Tomorrow.io API key is not configured.");
        }
        string url = string.Create(CultureInfo.InvariantCulture, $"v4/weather/forecast?location={latitude},{longitude}&timesteps=1h&units=metric&apikey={options.Value.ApiKey}");
        TomorrowIoResponse response = await httpClient.GetFromJsonAsync<TomorrowIoResponse>(url, cancellationToken) ?? throw new InvalidOperationException("Tomorrow.io returned an empty response.");
        return new Forecast
        {
            Source = Name,
            Latitude = latitude,
            Longitude = longitude,
            RetrievedAt = DateTimeOffset.UtcNow,
            Hourly = (response.Timelines?.Hourly ?? []).Select(MapHour).ToList()
        };
    }

    /// <summary>
    /// Maps a Tomorrow.io hourly interval to an hourly forecast.
    /// </summary>
    /// <param name="interval">The hourly interval.</param>
    private static HourlyForecast MapHour(TomorrowIoInterval interval)
    {
        TomorrowIoValues? values = interval.Values;

        // Rain, freezing rain, sleet and snow are separate rates; together they make the total precipitation.
        double?[] allRates = [values?.RainIntensity, values?.FreezingRainIntensity, values?.SleetIntensity, values?.SnowIntensity];
        List<double> rates = allRates.Where(x => x is not null).Select(x => x!.Value).ToList();
        return new HourlyForecast
        {
            Time = DateTimeOffset.Parse(interval.Time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            Condition = MapCondition(values?.WeatherCode),
            Temperature = values?.Temperature,
            FeelsLike = values?.TemperatureApparent,
            Humidity = values?.Humidity,
            Precipitation = rates.Count > 0 ? rates.Sum() : null,
            PrecipitationProbability = values?.PrecipitationProbability,
            WindSpeed = ToMph(values?.WindSpeed),
            WindGust = ToMph(values?.WindGust),
            WindDirection = values?.WindDirection,
            CloudCover = values?.CloudCover,
            UvIndex = values?.UvIndex
        };
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
    /// Maps a Tomorrow.io weather code to a normalised condition.
    /// </summary>
    /// <param name="code">The weather code.</param>
    private static WeatherCondition MapCondition(int? code)
    {
        return code switch
        {
            1000 or 1100 => WeatherCondition.Clear,
            1101 or 1102 => WeatherCondition.PartlyCloudy,
            1001 => WeatherCondition.Cloudy,
            2000 or 2100 => WeatherCondition.Fog,
            4000 => WeatherCondition.Drizzle,
            4001 or 4200 => WeatherCondition.Rain,
            4201 => WeatherCondition.HeavyRain,
            5000 or 5001 or 5100 or 5101 => WeatherCondition.Snow,
            6000 or 6001 or 6200 or 6201 => WeatherCondition.FreezingRain,
            7000 or 7101 or 7102 => WeatherCondition.Hail,
            8000 => WeatherCondition.Thunderstorm,
            _ => WeatherCondition.Unknown
        };
    }
}