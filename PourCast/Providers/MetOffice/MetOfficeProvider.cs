using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.MetOffice.Models;
using System.Globalization;

namespace PourCast.Providers.MetOffice;

/// <summary>
/// Weather provider backed by the Met Office DataHub site-specific API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for the Met Office.</param>
/// <param name="options">The Met Office options.</param>
/// <param name="cache">The memory cache.</param>
public class MetOfficeProvider(HttpClient httpClient, IOptions<MetOfficeOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "Met Office";

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
    /// Gets the forecast for a location from the Met Office, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"metoffice:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async (ICacheEntry entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the hourly and daily forecasts from the Met Office and maps them.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("The Met Office API key is not configured.");
        }

        // Fetch both time series at the same time.
        Task<MetOfficeResponse<MetOfficeHourly>> hourlyTask = GetAsync<MetOfficeHourly>("hourly", latitude, longitude, cancellationToken);
        Task<MetOfficeResponse<MetOfficeDaily>> dailyTask = GetAsync<MetOfficeDaily>("daily", latitude, longitude, cancellationToken);
        await Task.WhenAll(hourlyTask, dailyTask);
        MetOfficeFeature<MetOfficeHourly>? hourlyFeature = hourlyTask.Result.Features.FirstOrDefault();
        MetOfficeFeature<MetOfficeDaily>? dailyFeature = dailyTask.Result.Features.FirstOrDefault();

        // Map to the common forecast model.
        List<HourlyForecast> hourly = hourlyFeature?.Properties?.TimeSeries.Select(MapHourly).ToList() ?? [];

        // The Met Office labels rain with the end of the hour it fell in.
        PrecipitationTiming.ShiftToHourStart(hourly, false);
        List<double> coordinates = hourlyFeature?.Geometry?.Coordinates ?? [];
        return new Forecast
        {
            Source = Name,
            LocationName = hourlyFeature?.Properties?.Location?.Name,
            Latitude = coordinates.Count > 1 ? coordinates[1] : latitude,
            Longitude = coordinates.Count > 0 ? coordinates[0] : longitude,
            RetrievedAt = DateTimeOffset.UtcNow,
            Current = MapCurrent(hourlyFeature?.Properties?.TimeSeries ?? []),
            Hourly = hourly,
            Daily = MapDaily(dailyFeature?.Properties?.TimeSeries ?? [], hourly)
        };
    }

    /// <summary>
    /// Calls a site-specific time series endpoint.
    /// </summary>
    /// <typeparam name="T">The time series entry type.</typeparam>
    /// <param name="timeStep">The time step endpoint, e.g. hourly or daily.</param>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<MetOfficeResponse<T>> GetAsync<T>(string timeStep, double latitude, double longitude, CancellationToken cancellationToken)
    {
        string url = string.Create(CultureInfo.InvariantCulture, $"point/{timeStep}?latitude={latitude}&longitude={longitude}&excludeParameterMetadata=true&includeLocationName=true");
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Add("apikey", options.Value.ApiKey);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MetOfficeResponse<T>>(cancellationToken) ?? throw new InvalidOperationException("The Met Office returned an empty response.");
    }

    /// <summary>
    /// Builds current conditions from the most recent hourly entry.
    /// </summary>
    /// <param name="timeSeries">The hourly time series.</param>
    private static CurrentConditions? MapCurrent(List<MetOfficeHourly> timeSeries)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MetOfficeHourly? hour = timeSeries.LastOrDefault(x => ParseTime(x.Time) <= now) ?? timeSeries.FirstOrDefault();
        if (hour is null)
        {
            return null;
        }
        return new CurrentConditions
        {
            Time = ParseTime(hour.Time),
            Condition = MapCondition(hour.SignificantWeatherCode),
            IsDay = IsDay(hour.SignificantWeatherCode),
            Temperature = hour.ScreenTemperature,
            FeelsLike = hour.FeelsLikeTemperature,
            Humidity = hour.ScreenRelativeHumidity,
            Precipitation = hour.TotalPrecipAmount,
            Pressure = hour.Mslp / 100,
            WindSpeed = ToMph(hour.WindSpeed10m),
            WindGust = ToMph(hour.WindGustSpeed10m),
            WindDirection = hour.WindDirectionFrom10m
        };
    }

    /// <summary>
    /// Maps a Met Office hourly entry to an hourly forecast.
    /// </summary>
    /// <param name="hour">The hourly entry.</param>
    private static HourlyForecast MapHourly(MetOfficeHourly hour)
    {
        return new HourlyForecast
        {
            Time = ParseTime(hour.Time),
            Condition = MapCondition(hour.SignificantWeatherCode),
            Temperature = hour.ScreenTemperature,
            FeelsLike = hour.FeelsLikeTemperature,
            PrecipitationProbability = hour.ProbOfPrecipitation,
            Precipitation = hour.TotalPrecipAmount,
            Humidity = hour.ScreenRelativeHumidity,
            WindSpeed = ToMph(hour.WindSpeed10m),
            WindGust = ToMph(hour.WindGustSpeed10m),
            WindDirection = hour.WindDirectionFrom10m,
            UvIndex = hour.UvIndex
        };
    }

    /// <summary>
    /// Maps Met Office daily entries to daily forecasts, skipping past days.
    /// </summary>
    /// <param name="timeSeries">The daily time series.</param>
    /// <param name="hourly">The mapped hourly forecast, used to total precipitation.</param>
    private static List<DailyForecast> MapDaily(List<MetOfficeDaily> timeSeries, List<HourlyForecast> hourly)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        List<DailyForecast> result = [];
        foreach (MetOfficeDaily day in timeSeries)
        {
            DateOnly date = DateOnly.FromDateTime(ParseTime(day.Time).UtcDateTime);
            if (date < today)
            {
                continue;
            }
            List<HourlyForecast> hours = hourly.Where(x => DateOnly.FromDateTime(x.Time.UtcDateTime) == date && x.Precipitation is not null).ToList();
            result.Add(new DailyForecast
            {
                Date = date,
                Condition = MapCondition(day.DaySignificantWeatherCode ?? day.NightSignificantWeatherCode),
                TemperatureMax = day.DayMaxScreenTemperature,
                TemperatureMin = day.NightMinScreenTemperature,
                PrecipitationProbability = Max(day.DayProbabilityOfPrecipitation, day.NightProbabilityOfPrecipitation),
                PrecipitationTotal = hours.Count > 0 ? hours.Sum(x => x.Precipitation) : null,
                WindSpeedMax = ToMph(Max(day.Midday10MWindSpeed, day.Midnight10MWindSpeed)),
                WindGustMax = ToMph(Max(day.Midday10MWindGust, day.Midnight10MWindGust)),
                UvIndexMax = day.MaxUvIndex
            });
        }
        return result;
    }

    /// <summary>
    /// Parses a Met Office UTC time string.
    /// </summary>
    /// <param name="value">The time string, e.g. 2026-10-24T13:00Z.</param>
    private static DateTimeOffset ParseTime(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
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
    /// Returns the larger of two optional values.
    /// </summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    private static double? Max(double? first, double? second)
    {
        if (first is null)
        {
            return second;
        }
        if (second is null)
        {
            return first;
        }
        return Math.Max(first.Value, second.Value);
    }

    /// <summary>
    /// Works out whether a significant weather code is a daytime or night time variant.
    /// </summary>
    /// <param name="code">The significant weather code.</param>
    private static bool? IsDay(int? code)
    {
        return code switch
        {
            1 or 3 or 10 or 14 or 17 or 20 or 23 or 26 or 29 => true,
            0 or 2 or 9 or 13 or 16 or 19 or 22 or 25 or 28 => false,
            _ => null
        };
    }

    /// <summary>
    /// Maps a Met Office significant weather code to a normalised condition.
    /// </summary>
    /// <param name="code">The significant weather code.</param>
    private static WeatherCondition MapCondition(int? code)
    {
        return code switch
        {
            -1 or 11 => WeatherCondition.Drizzle,
            0 or 1 => WeatherCondition.Clear,
            2 or 3 => WeatherCondition.PartlyCloudy,
            5 or 6 => WeatherCondition.Fog,
            7 or 8 => WeatherCondition.Cloudy,
            9 or 10 or 13 or 14 => WeatherCondition.Showers,
            12 => WeatherCondition.Rain,
            15 => WeatherCondition.HeavyRain,
            16 or 17 or 18 => WeatherCondition.Sleet,
            19 or 20 or 21 => WeatherCondition.Hail,
            22 or 23 or 25 or 26 => WeatherCondition.SnowShowers,
            24 or 27 => WeatherCondition.Snow,
            28 or 29 or 30 => WeatherCondition.Thunderstorm,
            _ => WeatherCondition.Unknown
        };
    }
}