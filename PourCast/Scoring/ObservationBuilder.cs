using PourCast.Scoring.Data;
using PourCast.Scoring.Metar;
using PourCast.Services;
using System.Text.Json;

namespace PourCast.Scoring;

/// <summary>
/// Turns METAR reports into one observation per hour, lined up with forecast hours.
/// </summary>
public static class ObservationBuilder
{
    /// <summary>
    /// Knots to mph.
    /// </summary>
    private static readonly double KnotsToMph = 1.15078;

    /// <summary>
    /// Precipitation codes that count as rain, including snow, hail and thunderstorms.
    /// </summary>
    private static readonly string[] PrecipitationCodes = ["RA", "DZ", "SN", "SG", "PL", "GR", "GS", "UP", "SH", "TS"];

    /// <summary>
    /// Builds observations for each hour the reports fully cover. Temperature, humidity and wind come from the report closest
    /// to the start of the hour; rain from any report during the hour, or recent rain in the first report after it.
    /// </summary>
    /// <param name="station">The ICAO code of the station.</param>
    /// <param name="reports">The station's reports, oldest first.</param>
    /// <param name="combiningOptions">The feels like thresholds, so observed feels like matches the forecasts.</param>
    public static List<Observation> Build(string station, List<MetarReport> reports, CombiningOptions combiningOptions)
    {
        if (reports.Count == 0)
        {
            return [];
        }
        List<(DateTimeOffset Time, MetarReport Report)> value = [.. reports.Select(x => (DateTimeOffset.FromUnixTimeSeconds(x.ObservationTime), x))];
        List<(DateTimeOffset Time, MetarReport Report)> timed = value;
        DateTimeOffset latest = timed[^1].Time;
        List<DateTimeOffset> hours = [.. timed.Select(x => HourStart(x.Time.AddMinutes(30))).Distinct()];
        List<Observation> observations = [];
        foreach (DateTimeOffset hour in hours)
        {
            // Skip hours whose rain isn't known yet, as the next reports haven't arrived.
            if (latest < hour.AddHours(1))
            {
                continue;
            }
            (DateTimeOffset closestTime, MetarReport report) = timed.MinBy(x => Math.Abs((x.Time - hour).TotalMinutes));
            if (Math.Abs((closestTime - hour).TotalMinutes) > 30)
            {
                continue;
            }
            bool raining = timed.Any(x => x.Time > hour && x.Time <= hour.AddHours(1) && HasPrecipitation(x.Report.Weather)) || timed.Any(x => x.Time > hour.AddHours(1) && x.Time <= hour.AddMinutes(90) && HasRecentPrecipitation(x.Report.Raw));
            double? humidity = Humidity(report.Temperature, report.DewPoint);
            double? windSpeed = report.WindSpeed * KnotsToMph;
            observations.Add(new Observation { Station = station, Time = hour, Temperature = report.Temperature, Humidity = humidity, FeelsLike = FeelsLikeCalculator.Calculate(report.Temperature, windSpeed, humidity, combiningOptions), WindSpeed = windSpeed, WindGust = report.WindGust * KnotsToMph, WindDirection = WindDirection(report), Raining = raining });
        }
        return observations;
    }

    /// <summary>
    /// The start of the hour a time falls in, in UTC.
    /// </summary>
    /// <param name="time">The time.</param>
    private static DateTimeOffset HourStart(DateTimeOffset time)
    {
        return new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, 0, 0, TimeSpan.Zero);
    }

    /// <summary>
    /// Whether present weather codes include precipitation at the station. Vicinity codes such as VCSH don't count.
    /// </summary>
    /// <param name="weather">The present weather codes, e.g. "-RA BR".</param>
    private static bool HasPrecipitation(string? weather)
    {
        return !string.IsNullOrWhiteSpace(weather) && weather.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => !x.StartsWith("VC") && PrecipitationCodes.Any(code => x.Contains(code)));
    }

    /// <summary>
    /// Whether the raw METAR reports recent precipitation since the last report, e.g. RERA or RESHRA.
    /// </summary>
    /// <param name="raw">The raw METAR text.</param>
    private static bool HasRecentPrecipitation(string? raw)
    {
        return !string.IsNullOrWhiteSpace(raw) && raw.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith("RE") && x.Length > 2 && PrecipitationCodes.Any(code => x[2..].Contains(code)));
    }

    /// <summary>
    /// Relative humidity in % from temperature and dew point, using the Magnus formula.
    /// </summary>
    /// <param name="temperature">Temperature in °C.</param>
    /// <param name="dewPoint">Dew point in °C.</param>
    private static double? Humidity(double? temperature, double? dewPoint)
    {
        if (temperature is null || dewPoint is null)
        {
            return null;
        }
        return Math.Min(100, 100 * Math.Exp(17.625 * dewPoint.Value / (243.04 + dewPoint.Value)) / Math.Exp(17.625 * temperature.Value / (243.04 + temperature.Value)));
    }

    /// <summary>
    /// Wind direction in degrees, or null when calm or variable.
    /// </summary>
    /// <param name="report">The report.</param>
    private static double? WindDirection(MetarReport report)
    {
        if (report.WindSpeed is null or 0.0 || report.WindDirection is not JsonElement direction || direction.ValueKind != JsonValueKind.Number)
        {
            return null;
        }
        return direction.GetDouble();
    }
}