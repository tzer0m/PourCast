using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.OpenMeteo.Models;
using System.Globalization;

namespace PourCast.Providers.OpenMeteo;

/// <summary>
/// Weather provider backed by the Open-Meteo forecast API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for Open-Meteo.</param>
/// <param name="options">The Open-Meteo options.</param>
/// <param name="cache">The memory cache.</param>
public class OpenMeteoProvider(HttpClient httpClient, IOptions<OpenMeteoOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "Open-Meteo";

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
    /// Gets the forecast for a location from Open-Meteo, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"openmeteo:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async (ICacheEntry entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the forecast from Open-Meteo and maps it.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        // Request the forecast in UK units with UTC times, so hours stay correct across clock changes.
        string current = "temperature_2m,apparent_temperature,relative_humidity_2m,is_day,precipitation,weather_code,cloud_cover,pressure_msl,wind_speed_10m,wind_direction_10m,wind_gusts_10m";
        string hourly = "temperature_2m,apparent_temperature,precipitation_probability,precipitation,weather_code,relative_humidity_2m,cloud_cover,wind_speed_10m,wind_direction_10m,wind_gusts_10m,uv_index,uv_index_clear_sky";
        string daily = "weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,precipitation_sum,wind_speed_10m_max,wind_gusts_10m_max,uv_index_max,sunrise,sunset";
        string url = string.Create(CultureInfo.InvariantCulture, $"v1/forecast?latitude={latitude}&longitude={longitude}&current={current}&hourly={hourly}&daily={daily}&timezone=GMT&wind_speed_unit=mph&forecast_days={options.Value.ForecastDays}");
        OpenMeteoResponse response = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken) ?? throw new InvalidOperationException("Open-Meteo returned an empty response.");
        TimeSpan offset = TimeSpan.FromSeconds(response.UtcOffsetSeconds);

        // Map the response to the common forecast model.
        return new Forecast
        {
            Source = Name,
            Latitude = response.Latitude,
            Longitude = response.Longitude,
            RetrievedAt = DateTimeOffset.UtcNow,
            Current = MapCurrent(response.Current, offset),
            Hourly = MapHourly(response.Hourly, offset),
            Daily = MapDaily(response.Daily, offset)
        };
    }

    /// <summary>
    /// Maps the Open-Meteo current block to current conditions.
    /// </summary>
    /// <param name="current">The current block.</param>
    /// <param name="offset">The location's UTC offset.</param>
    private static CurrentConditions? MapCurrent(OpenMeteoCurrent? current, TimeSpan offset)
    {
        if (current is null)
        {
            return null;
        }
        return new CurrentConditions
        {
            Time = ParseTime(current.Time, offset),
            Condition = WmoCodes.ToCondition(current.WeatherCode),
            IsDay = current.IsDay is null ? null : current.IsDay == 1,
            Temperature = current.Temperature,
            FeelsLike = current.ApparentTemperature,
            Humidity = current.RelativeHumidity,
            Precipitation = current.Precipitation,
            CloudCover = current.CloudCover,
            Pressure = current.Pressure,
            WindSpeed = current.WindSpeed,
            WindGust = current.WindGusts,
            WindDirection = current.WindDirection
        };
    }

    /// <summary>
    /// Maps the Open-Meteo hourly arrays to hourly forecasts.
    /// </summary>
    /// <param name="hourly">The hourly arrays.</param>
    /// <param name="offset">The location's UTC offset.</param>
    private static List<HourlyForecast> MapHourly(OpenMeteoHourly? hourly, TimeSpan offset)
    {
        List<HourlyForecast> result = [];
        if (hourly is null)
        {
            return result;
        }
        for (int i = 0; i < hourly.Time.Count; i++)
        {
            result.Add(new HourlyForecast
            {
                Time = ParseTime(hourly.Time[i], offset),
                Condition = WmoCodes.ToCondition(At(hourly.WeatherCode, i)),
                Temperature = At(hourly.Temperature, i),
                FeelsLike = At(hourly.ApparentTemperature, i),
                PrecipitationProbability = At(hourly.PrecipitationProbability, i),
                Precipitation = At(hourly.Precipitation, i),
                Humidity = At(hourly.RelativeHumidity, i),
                CloudCover = At(hourly.CloudCover, i),
                WindSpeed = At(hourly.WindSpeed, i),
                WindGust = At(hourly.WindGusts, i),
                WindDirection = At(hourly.WindDirection, i),
                UvIndex = At(hourly.UvIndex, i),
                UvIndexClearSky = At(hourly.UvIndexClearSky, i)
            });
        }
        return result;
    }

    /// <summary>
    /// Maps the Open-Meteo daily arrays to daily forecasts.
    /// </summary>
    /// <param name="daily">The daily arrays.</param>
    /// <param name="offset">The location's UTC offset.</param>
    private static List<DailyForecast> MapDaily(OpenMeteoDaily? daily, TimeSpan offset)
    {
        List<DailyForecast> result = [];
        if (daily is null)
        {
            return result;
        }
        for (int i = 0; i < daily.Time.Count; i++)
        {
            string? sunrise = At(daily.Sunrise, i);
            string? sunset = At(daily.Sunset, i);
            result.Add(new DailyForecast
            {
                Date = DateOnly.Parse(daily.Time[i], CultureInfo.InvariantCulture),
                Condition = WmoCodes.ToCondition(At(daily.WeatherCode, i)),
                TemperatureMax = At(daily.TemperatureMax, i),
                TemperatureMin = At(daily.TemperatureMin, i),
                PrecipitationProbability = At(daily.PrecipitationProbabilityMax, i),
                PrecipitationTotal = At(daily.PrecipitationSum, i),
                WindSpeedMax = At(daily.WindSpeedMax, i),
                WindGustMax = At(daily.WindGustsMax, i),
                UvIndexMax = At(daily.UvIndexMax, i),
                Sunrise = string.IsNullOrEmpty(sunrise) ? null : ParseTime(sunrise, offset),
                Sunset = string.IsNullOrEmpty(sunset) ? null : ParseTime(sunset, offset)
            });
        }
        return result;
    }

    /// <summary>
    /// Parses an Open-Meteo local time string into a time with offset.
    /// </summary>
    /// <param name="value">The local time string.</param>
    /// <param name="offset">The location's UTC offset.</param>
    private static DateTimeOffset ParseTime(string value, TimeSpan offset)
    {
        DateTime local = DateTime.Parse(value, CultureInfo.InvariantCulture);
        return new DateTimeOffset(local, offset);
    }

    /// <summary>
    /// Gets an item from a list, or the default if the index is out of range.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="values">The list of values.</param>
    /// <param name="index">The index to read.</param>
    private static T? At<T>(List<T> values, int index)
    {
        return index < values.Count ? values[index] : default;
    }
}