using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Providers.OpenMeteo;
using System.Globalization;
using System.Text.Json;

namespace PourCast.Providers.OpenMeteoEnsemble;

/// <summary>
/// Weather provider backed by the Open-Meteo ensemble API, used for long range forecasts.
/// </summary>
/// <param name="httpClient">The HTTP client configured for the ensemble API.</param>
/// <param name="options">The ensemble options.</param>
/// <param name="cache">The memory cache.</param>
public class OpenMeteoEnsembleProvider(HttpClient httpClient, IOptions<OpenMeteoEnsembleOptions> options, IMemoryCache cache) : IWeatherProvider
{
    /// <summary>
    /// The display name of the provider.
    /// </summary>
    public string Name => "Open-Meteo Ensemble";

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
    /// Gets the ensemble forecast for a location, using the cache where possible.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"openmeteoensemble:{latitude:F3}:{longitude:F3}");
        Forecast? forecast = await cache.GetOrCreateAsync(key, async (ICacheEntry entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            return await FetchForecastAsync(latitude, longitude, cancellationToken);
        });
        return forecast!;
    }

    /// <summary>
    /// Fetches the ensemble forecast and returns one hourly entry per member per hour.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Forecast> FetchForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        // Every member comes back as its own field, e.g. temperature_2m and temperature_2m_member01.
        string hourly = "temperature_2m,apparent_temperature,relative_humidity_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m,wind_gusts_10m";
        string url = string.Create(CultureInfo.InvariantCulture, $"v1/ensemble?latitude={latitude}&longitude={longitude}&models={options.Value.Model}&hourly={hourly}&timezone=GMT&wind_speed_unit=mph&forecast_days={options.Value.ForecastDays}");
        using JsonDocument document = await httpClient.GetFromJsonAsync<JsonDocument>(url, cancellationToken) ?? throw new InvalidOperationException("Open-Meteo Ensemble returned an empty response.");
        JsonElement root = document.RootElement;
        JsonElement hourlyElement = root.GetProperty("hourly");
        List<DateTimeOffset> times = hourlyElement.GetProperty("time").EnumerateArray().Select(x => new DateTimeOffset(DateTime.Parse(x.GetString()!, CultureInfo.InvariantCulture), TimeSpan.Zero)).ToList();

        // Group the fields by member suffix so each member can be built as its own set of hours.
        Dictionary<string, Dictionary<string, JsonElement>> members = [];
        foreach (JsonProperty property in hourlyElement.EnumerateObject().Where(x => x.Name != "time"))
        {
            int index = property.Name.IndexOf("_member", StringComparison.Ordinal);
            string variable = index < 0 ? property.Name : property.Name[..index];
            string member = index < 0 ? "control" : property.Name[(index + 1)..];
            if (!members.TryGetValue(member, out Dictionary<string, JsonElement>? fields))
            {
                fields = [];
                members[member] = fields;
            }
            fields[variable] = property.Value;
        }

        // Each member's hour is added separately, so combining hours gives the mean and the spread across members.
        List<HourlyForecast> result = [];
        foreach ((string member, Dictionary<string, JsonElement> fields) in members)
        {
            for (int i = 0; i < times.Count; i++)
            {
                double? precipitation = Value(fields, "precipitation", i);
                double? code = Value(fields, "weather_code", i);
                result.Add(new HourlyForecast
                {
                    Time = times[i],
                    Member = member,
                    Condition = WmoCodes.ToCondition(code is null ? null : (int)code.Value),
                    Temperature = Value(fields, "temperature_2m", i),
                    FeelsLike = Value(fields, "apparent_temperature", i),
                    Humidity = Value(fields, "relative_humidity_2m", i),
                    Precipitation = precipitation,
                    PrecipitationProbability = precipitation is null ? null : precipitation >= 0.1 ? 100 : 0,
                    WindSpeed = Value(fields, "wind_speed_10m", i),
                    WindGust = Value(fields, "wind_gusts_10m", i),
                    WindDirection = Value(fields, "wind_direction_10m", i)
                });
            }
        }
        return new Forecast
        {
            Source = Name,
            Latitude = root.GetProperty("latitude").GetDouble(),
            Longitude = root.GetProperty("longitude").GetDouble(),
            RetrievedAt = DateTimeOffset.UtcNow,
            IsEnsemble = true,
            Hourly = result.Where(x => x.Temperature is not null).ToList()
        };
    }

    /// <summary>
    /// Reads one value from a member's field, or null if it is missing.
    /// </summary>
    /// <param name="fields">The member's fields by variable name.</param>
    /// <param name="variable">The variable name.</param>
    /// <param name="index">The hour index.</param>
    private static double? Value(Dictionary<string, JsonElement> fields, string variable, int index)
    {
        if (!fields.TryGetValue(variable, out JsonElement values) || index >= values.GetArrayLength())
        {
            return null;
        }
        JsonElement value = values[index];
        return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
    }
}