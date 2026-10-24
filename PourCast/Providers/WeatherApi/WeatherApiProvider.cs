using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.WeatherApi.Models;
using System.Globalization;

namespace PourCast.Providers.WeatherApi;

/// <summary>
/// Weather provider backed by the WeatherAPI.com forecast API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for WeatherAPI.com.</param>
/// <param name="options">The WeatherAPI.com options.</param>
/// <param name="cache">The memory cache.</param>
public class WeatherApiProvider(HttpClient httpClient, IOptions<WeatherApiOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "WeatherAPI.com";

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
    /// Gets the forecast for a location from WeatherAPI.com, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"weatherapi:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async (ICacheEntry entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the hourly forecast from WeatherAPI.com and maps it.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("The WeatherAPI.com API key is not configured.");
        }
        string url = string.Create(CultureInfo.InvariantCulture, $"v1/forecast.json?key={options.Value.ApiKey}&q={latitude},{longitude}&days={options.Value.ForecastDays}&aqi=no&alerts=no");
        WeatherApiResponse response = await httpClient.GetFromJsonAsync<WeatherApiResponse>(url, cancellationToken) ?? throw new InvalidOperationException("WeatherAPI.com returned an empty response.");
        return new Forecast
        {
            Source = Name,
            LocationName = response.Location?.Name,
            Latitude = response.Location?.Lat ?? latitude,
            Longitude = response.Location?.Lon ?? longitude,
            RetrievedAt = DateTimeOffset.UtcNow,
            Hourly = (response.Forecast?.ForecastDay ?? []).SelectMany(x => x.Hour).Select(MapHour).ToList()
        };
    }

    /// <summary>
    /// Maps a WeatherAPI.com hour to an hourly forecast.
    /// </summary>
    /// <param name="hour">The hour.</param>
    private static HourlyForecast MapHour(WeatherApiHour hour)
    {
        return new HourlyForecast
        {
            Time = DateTimeOffset.FromUnixTimeSeconds(hour.TimeEpoch),
            Condition = MapCondition(hour.Condition?.Code),
            Temperature = hour.TempC,
            FeelsLike = hour.FeelsLikeC,
            Humidity = hour.Humidity,
            CloudCover = hour.Cloud,
            Precipitation = hour.PrecipMm,
            PrecipitationProbability = hour.ChanceOfRain is null && hour.ChanceOfSnow is null ? null : Math.Max(hour.ChanceOfRain ?? 0, hour.ChanceOfSnow ?? 0),
            WindSpeed = hour.WindMph,
            WindGust = hour.GustMph,
            WindDirection = hour.WindDegree,
            UvIndex = hour.Uv
        };
    }

    /// <summary>
    /// Maps a WeatherAPI.com condition code to a normalised condition.
    /// </summary>
    /// <param name="code">The condition code.</param>
    private static WeatherCondition MapCondition(int? code)
    {
        return code switch
        {
            1000 => WeatherCondition.Clear,
            1003 => WeatherCondition.PartlyCloudy,
            1006 or 1009 => WeatherCondition.Cloudy,
            1030 or 1135 or 1147 => WeatherCondition.Fog,
            1150 or 1153 => WeatherCondition.Drizzle,
            1072 or 1168 or 1171 or 1198 or 1201 => WeatherCondition.FreezingRain,
            1180 or 1183 or 1186 or 1189 => WeatherCondition.Rain,
            1192 or 1195 => WeatherCondition.HeavyRain,
            1063 or 1240 or 1243 or 1246 => WeatherCondition.Showers,
            1069 or 1204 or 1207 or 1249 or 1252 => WeatherCondition.Sleet,
            1237 or 1261 or 1264 => WeatherCondition.Hail,
            1066 or 1255 or 1258 => WeatherCondition.SnowShowers,
            1114 or 1117 or 1210 or 1213 or 1216 or 1219 or 1222 or 1225 => WeatherCondition.Snow,
            1087 or 1273 or 1276 or 1279 or 1282 => WeatherCondition.Thunderstorm,
            _ => WeatherCondition.Unknown
        };
    }
}