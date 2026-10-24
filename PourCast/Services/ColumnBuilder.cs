using PourCast.Models;

namespace PourCast.Services;

/// <summary>
/// Works out the time windows that make up chart columns.
/// </summary>
public static class ColumnBuilder
{
    /// <summary>
    /// One window per hour covered by any of the forecasts.
    /// </summary>
    /// <param name="forecasts">The forecasts being combined.</param>
    public static List<ColumnWindow> Hourly(List<Forecast> forecasts)
    {
        return forecasts.SelectMany(x => x.Hourly).Select(x => x.Time.ToUniversalTime()).Distinct().Order().Select(x => new ColumnWindow { Start = x, End = x.AddHours(1) }).ToList();
    }

    /// <summary>
    /// The windows for a range's chart, starting at 6am today on the local clock: 6 hour columns for 7 days, or daily columns for 30 days.
    /// </summary>
    /// <param name="range">The forecast range.</param>
    /// <param name="timeZone">The viewer's time zone, so columns line up with their clock.</param>
    /// <param name="now">The current time.</param>
    public static List<ColumnWindow> ForRange(ForecastRange range, TimeZoneInfo timeZone, DateTimeOffset now)
    {
        int stepHours = range == ForecastRange.Month ? 24 : 6;
        int count = range == ForecastRange.Month ? 30 : 28;
        DateTime today = TimeZoneInfo.ConvertTime(now, timeZone).Date;
        List<DateTimeOffset> edges = Enumerable.Range(0, count + 1).Select(x => ToUtc(today.AddHours(6 + x * stepHours), timeZone)).ToList();
        return Enumerable.Range(0, count).Select(x => new ColumnWindow { Start = edges[x], End = edges[x + 1] }).ToList();
    }

    /// <summary>
    /// Converts a local clock time to UTC, nudging past any time skipped by a clock change.
    /// </summary>
    /// <param name="local">The local clock time.</param>
    /// <param name="timeZone">The time zone.</param>
    private static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo timeZone)
    {
        DateTime unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone), TimeSpan.Zero);
    }
}