using PourCast.Models;

namespace PourCast.Services;

/// <summary>
/// Builds the current, hourly and daily weather shown by the Tidgets home screen widget.
/// </summary>
/// <param name="weatherService">The weather service.</param>
/// <param name="configuration">Used to read what counts as rain and how far ahead to look.</param>
public class WidgetService(WeatherService weatherService, IConfiguration configuration)
{
    /// <summary>
    /// Gets the widget weather for a place.
    /// </summary>
    /// <param name="latitude">The latitude of the place.</param>
    /// <param name="longitude">The longitude of the place.</param>
    /// <param name="name">The place name to show, or null to use the one the sources give.</param>
    /// <param name="timeZone">The place's IANA time zone, or null for UTC.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<WidgetResponse> GetAsync(double latitude, double longitude, string? name, string? timeZone, CancellationToken cancellationToken)
    {
        TimeZoneInfo zone = !string.IsNullOrWhiteSpace(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out TimeZoneInfo? found) ? found : TimeZoneInfo.Utc;
        WeatherReport report = await weatherService.GetReportAsync(latitude, longitude, ForecastRange.Week, zone, cancellationToken);

        // The current hour is the first one that hasn't ended; high and low cover the next 24 hours.
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<HourlySummary> upcoming = [.. report.Hourly.Where(x => x.End > now).OrderBy(x => x.Time)];
        HourlySummary? current = upcoming.FirstOrDefault();
        List<double> next24 = [.. upcoming.Where(x => x.Time < now.AddHours(24) && x.TemperatureAverage is not null).Select(x => x.TemperatureAverage!.Value)];

        // Rain is any hour whose combined precipitation reaches the threshold, looking ahead a set number of hours.
        double rainThreshold = configuration.GetValue("Widget:RainThreshold", 0.1);
        List<HourlySummary> rainWindow = [.. upcoming.Where(x => x.Time < now.AddHours(configuration.GetValue("Widget:RainHours", 48)))];
        Func<HourlySummary, bool> isWet = x => (x.PrecipitationAverage ?? 0) >= rainThreshold;
        bool isRaining = rainWindow.Count > 0 && isWet(rainWindow[0]);

        // Days after today, grouped by the place's own calendar.
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        List<WidgetDay> daily = [.. report.Hourly.Where(x => x.TemperatureAverage is not null).GroupBy(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.Time, zone).DateTime)).Where(x => x.Key > today).OrderBy(x => x.Key).Take(5).Select(x => ToDay(x.Key, [.. x], zone))];
        return new WidgetResponse
        {
            Place = !string.IsNullOrWhiteSpace(name) ? name : report.LocationName ?? "PourCast",
            Temperature = current?.TemperatureAverage,
            Condition = (current?.Condition ?? WeatherCondition.Unknown).ToString(),
            IsDay = IsDay(report, now),
            IsRaining = isRaining,
            RainStart = isRaining ? null : rainWindow.FirstOrDefault(isWet)?.Time,
            RainEnd = isRaining ? rainWindow.FirstOrDefault(x => !isWet(x))?.Time : null,
            High = next24.Count > 0 ? next24.Max() : null,
            Low = next24.Count > 0 ? next24.Min() : null,
            Hourly = [.. upcoming.Take(6).Select(x => new WidgetHour { Time = x.Time, Temperature = x.TemperatureAverage, Condition = x.Condition.ToString(), IsDay = IsDay(report, x.Time) })],
            Daily = daily
        };
    }

    /// <summary>
    /// Summarises a day's hours as a low, a high and its main daytime condition.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <param name="hours">The day's hours, all with a temperature.</param>
    /// <param name="zone">The place's time zone.</param>
    private static WidgetDay ToDay(DateOnly date, List<HourlySummary> hours, TimeZoneInfo zone)
    {
        // The condition is the commonest one between 7am and 7pm, with the more severe weather winning a tie.
        List<HourlySummary> daytime = [.. hours.Where(x => TimeZoneInfo.ConvertTime(x.Time, zone).Hour is >= 7 and < 19)];
        WeatherCondition condition = (daytime.Count > 0 ? daytime : hours).Where(x => x.Condition != WeatherCondition.Unknown).GroupBy(x => x.Condition).OrderByDescending(x => x.Count()).ThenByDescending(x => x.Key).Select(x => x.Key).FirstOrDefault();
        return new WidgetDay { Date = date, Condition = condition.ToString(), Low = hours.Min(x => x.TemperatureAverage), High = hours.Max(x => x.TemperatureAverage), UvIndex = hours.Max(x => x.UvIndexAverage) };
    }

    /// <summary>
    /// Works out whether the sun is up at a time, from the report's sunrise and sunset times.
    /// </summary>
    /// <param name="report">The weather report.</param>
    /// <param name="time">The time to check.</param>
    private static bool IsDay(WeatherReport report, DateTimeOffset time)
    {
        DateOnly date = DateOnly.FromDateTime(time.UtcDateTime);
        return report.Daylight.Any(x => (x.AlwaysUp && x.Date == date) || (x.Sunrise is not null && x.Sunset is not null && time >= x.Sunrise && time < x.Sunset));
    }
}