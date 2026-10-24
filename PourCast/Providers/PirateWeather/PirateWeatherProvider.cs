using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.PirateWeather.Models;
using System.Globalization;

namespace PourCast.Providers.PirateWeather;

/// <summary>
/// Weather provider backed by the Pirate Weather API (Dark Sky compatible, built on NOAA models).
/// </summary>
/// <param name="httpClient">The HTTP client configured for Pirate Weather.</param>
/// <param name="options">The Pirate Weather options.</param>
/// <param name="cache">The memory cache.</param>
public class PirateWeatherProvider(HttpClient httpClient, IOptions<PirateWeatherOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "Pirate Weather";

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
    /// Gets the forecast for a location from Pirate Weather, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"pirateweather:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the hourly forecast from Pirate Weather and maps it.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("The Pirate Weather API key is not configured.");
        }

        // UK units give °C, mph and mm; extend=hourly returns 7 days of hours instead of 2.
        string url = string.Create(CultureInfo.InvariantCulture, $"forecast/{options.Value.ApiKey}/{latitude},{longitude}?units=uk&extend=hourly&exclude=currently,minutely,daily,alerts");
        PirateWeatherResponse response = await httpClient.GetFromJsonAsync<PirateWeatherResponse>(url, cancellationToken) ?? throw new InvalidOperationException("Pirate Weather returned an empty response.");
        return new Forecast
        {
            Source = Name,
            Latitude = response.Latitude,
            Longitude = response.Longitude,
            RetrievedAt = DateTimeOffset.UtcNow,
            Hourly = (response.Hourly?.Data ?? []).Select(MapHour).ToList()
        };
    }

    /// <summary>
    /// Maps a Pirate Weather data point to an hourly forecast, converting fractions to percentages.
    /// </summary>
    /// <param name="point">The data point.</param>
    private static HourlyForecast MapHour(PirateWeatherDataPoint point)
    {
        return new HourlyForecast
        {
            Time = DateTimeOffset.FromUnixTimeSeconds(point.Time),
            Condition = MapCondition(point.Icon),
            Temperature = point.Temperature,
            FeelsLike = point.ApparentTemperature,
            Humidity = point.Humidity * 100,
            Precipitation = point.PrecipIntensity,
            PrecipitationProbability = point.PrecipProbability * 100,
            WindSpeed = point.WindSpeed,
            WindGust = point.WindGust,
            WindDirection = point.WindBearing,
            CloudCover = point.CloudCover * 100,
            UvIndex = point.UvIndex
        };
    }

    /// <summary>
    /// Maps a Pirate Weather icon name to a normalised condition.
    /// </summary>
    /// <param name="icon">The icon name.</param>
    private static WeatherCondition MapCondition(string? icon)
    {
        return icon switch
        {
            "clear-day" or "clear-night" => WeatherCondition.Clear,
            "partly-cloudy-day" or "partly-cloudy-night" => WeatherCondition.PartlyCloudy,
            "cloudy" => WeatherCondition.Cloudy,
            "fog" => WeatherCondition.Fog,
            "rain" => WeatherCondition.Rain,
            "sleet" => WeatherCondition.Sleet,
            "snow" => WeatherCondition.Snow,
            "hail" => WeatherCondition.Hail,
            "thunderstorm" => WeatherCondition.Thunderstorm,
            "wind" => WeatherCondition.Cloudy,
            _ => WeatherCondition.Unknown
        };
    }
}