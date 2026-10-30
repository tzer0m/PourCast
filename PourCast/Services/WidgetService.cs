using PourCast.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PourCast.Services;

/// <summary>
/// Builds the compact weather summary shown by the Tidgets home screen widget.
/// </summary>
/// <param name="weatherService">The weather service.</param>
public partial class WidgetService(WeatherService weatherService)
{
    /// <summary>
    /// Gets the widget summary for a place.
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
        List<double> temperatures = [.. upcoming.Where(x => x.Time < now.AddHours(24) && x.TemperatureAverage is not null).Select(x => x.TemperatureAverage!.Value)];
        List<string> parts = [];
        if (current is not null && current.Condition != WeatherCondition.Unknown)
        {
            parts.Add(WordBoundary().Replace(current.Condition.ToString(), " "));
        }
        if (temperatures.Count > 0)
        {
            parts.Add($"H {Format(temperatures.Max())} L {Format(temperatures.Min())}");
        }
        string title = !string.IsNullOrWhiteSpace(name) ? name : report.LocationName ?? "PourCast";
        string value = current?.TemperatureAverage is double temperature ? $"{Format(temperature)}C" : "--";
        return new WidgetResponse { Title = title, Value = value, Subtitle = string.Join(" · ", parts) };
    }

    /// <summary>
    /// Formats a temperature as a whole number with a degree sign.
    /// </summary>
    /// <param name="temperature">The temperature in °C.</param>
    private static string Format(double temperature)
    {
        // Adding zero turns a rounded -0 into 0.
        return string.Create(CultureInfo.InvariantCulture, $"{Math.Round(temperature, MidpointRounding.AwayFromZero) + 0d}°");
    }

    /// <summary>
    /// Matches the gap between a lowercase and an uppercase letter, to split a name like PartlyCloudy into words.
    /// </summary>
    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}