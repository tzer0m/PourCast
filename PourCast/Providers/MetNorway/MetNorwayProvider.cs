using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.MetNorway.Models;
using System.Globalization;

namespace PourCast.Providers.MetNorway;

/// <summary>
/// Weather provider backed by the MET Norway (Yr) locationforecast API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for MET Norway.</param>
/// <param name="options">The MET Norway options.</param>
/// <param name="cache">The memory cache.</param>
public class MetNorwayProvider(HttpClient httpClient, IOptions<MetNorwayOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "MET Norway";

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
    /// Gets the forecast for a location from MET Norway, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"metnorway:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async (ICacheEntry entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the forecast from MET Norway and maps the hourly steps.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        // MET Norway rejects coordinates with more than 4 decimal places, and requests without an identifying User-Agent.
        string url = string.Create(CultureInfo.InvariantCulture, $"weatherapi/locationforecast/2.0/complete?lat={latitude:F4}&lon={longitude:F4}");
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", options.Value.UserAgent);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        MetNorwayResponse result = await response.Content.ReadFromJsonAsync<MetNorwayResponse>(cancellationToken) ?? throw new InvalidOperationException("MET Norway returned an empty response.");

        // Only steps with a following hour are hourly; later steps are 6 hourly and are left out.
        List<HourlyForecast> hourly = (result.Properties?.Timeseries ?? []).Where(x => x.Data?.NextOneHours is not null).Select(MapHour).ToList();
        return new Forecast
        {
            Source = Name,
            Latitude = latitude,
            Longitude = longitude,
            RetrievedAt = DateTimeOffset.UtcNow,
            Hourly = hourly
        };
    }

    /// <summary>
    /// Maps a MET Norway hourly step to an hourly forecast.
    /// </summary>
    /// <param name="step">The time step.</param>
    private static HourlyForecast MapHour(MetNorwayTimeStep step)
    {
        MetNorwayInstantDetails? instant = step.Data?.Instant?.Details;
        MetNorwayPeriod? hour = step.Data?.NextOneHours;
        return new HourlyForecast
        {
            Time = DateTimeOffset.Parse(step.Time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            Condition = MapCondition(hour?.Summary?.SymbolCode),
            Temperature = instant?.AirTemperature,
            Humidity = instant?.RelativeHumidity,
            CloudCover = instant?.CloudAreaFraction,
            WindSpeed = ToMph(instant?.WindSpeed),
            WindGust = ToMph(instant?.WindSpeedOfGust),
            WindDirection = instant?.WindFromDirection,
            UvIndexClearSky = instant?.UltravioletIndexClearSky,
            Precipitation = hour?.Details?.PrecipitationAmount,
            PrecipitationProbability = hour?.Details?.ProbabilityOfPrecipitation
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
    /// Maps a MET Norway symbol code (e.g. lightrainshowers_day) to a normalised condition.
    /// </summary>
    /// <param name="symbol">The symbol code.</param>
    private static WeatherCondition MapCondition(string? symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return WeatherCondition.Unknown;
        }
        string code = symbol.Split('_')[0];
        if (code.Contains("thunder"))
        {
            return WeatherCondition.Thunderstorm;
        }
        return code switch
        {
            "clearsky" => WeatherCondition.Clear,
            "fair" or "partlycloudy" => WeatherCondition.PartlyCloudy,
            "cloudy" => WeatherCondition.Cloudy,
            "fog" => WeatherCondition.Fog,
            "lightrain" or "rain" => WeatherCondition.Rain,
            "heavyrain" => WeatherCondition.HeavyRain,
            "lightrainshowers" or "rainshowers" or "heavyrainshowers" => WeatherCondition.Showers,
            "lightsleet" or "sleet" or "heavysleet" or "lightsleetshowers" or "sleetshowers" or "heavysleetshowers" => WeatherCondition.Sleet,
            "lightsnow" or "snow" or "heavysnow" => WeatherCondition.Snow,
            "lightsnowshowers" or "snowshowers" or "heavysnowshowers" => WeatherCondition.SnowShowers,
            _ => WeatherCondition.Unknown
        };
    }
}