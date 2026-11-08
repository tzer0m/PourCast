using PourCast.Models;
using PourCast.Scoring.Data;
using PourCast.Scoring.Models;
using PourCast.Services;

namespace PourCast.Scoring;

/// <summary>
/// Builds the weighted forecast: each source has its usual error taken off, then the sources are averaged with more weight on the ones that have been more accurate at that day ahead.
/// </summary>
public static class WeightedCombiner
{
    /// <summary>
    /// Builds the weighted forecast's hourly predictions for a snapshot.
    /// </summary>
    /// <param name="forecasts">Every source's forecast.</param>
    /// <param name="corrections">Each source's correction by day ahead and metric.</param>
    /// <param name="issuedAt">When the snapshot was taken.</param>
    /// <param name="end">The end of the snapshot's predictions.</param>
    /// <param name="settings">The scoring options.</param>
    /// <param name="combining">The combining options, for feels like.</param>
    public static List<Prediction> Combine(List<Forecast> forecasts, Dictionary<(string Source, int Day, ScoreMetric Metric), SourceCorrection> corrections, DateTimeOffset issuedAt, DateTimeOffset end, ScoringOptions settings, CombiningOptions combining)
    {
        List<Prediction> result = [];
        foreach (IGrouping<DateTimeOffset, (string Source, HourlyForecast Hour)> hour in forecasts.SelectMany(f => f.Hourly.Where(x => x.Member is null && x.Time >= issuedAt && x.Time < end).Select(x => (f.Source, Hour: x))).GroupBy(x => x.Hour.Time.ToUniversalTime()).OrderBy(x => x.Key))
        {
            int leadHours = (int)Math.Round((hour.Key - issuedAt).TotalHours);
            int day = leadHours / 24 + 1;
            List<(string Source, HourlyForecast Hour)> values = [.. hour];
            double? temperature = Blend(values, x => x.Temperature, ScoreMetric.Temperature, day, corrections, settings);
            if (temperature is null)
            {
                continue;
            }

            // Feels like is worked out for each source the same way its snapshot is, then corrected like any other metric.
            double? feelsLike = Blend(values, x => combining.CalculateFeelsLike ? FeelsLikeCalculator.Calculate(x.Temperature, x.WindSpeed, x.Humidity, combining) : x.FeelsLike, ScoreMetric.FeelsLike, day, corrections, settings);
            double? humidity = Blend(values, x => x.Humidity, ScoreMetric.Humidity, day, corrections, settings);
            double? windSpeed = Blend(values, x => x.WindSpeed, ScoreMetric.WindSpeed, day, corrections, settings);
            double? windGust = Blend(values, x => x.WindGust, ScoreMetric.WindGust, day, corrections, settings);
            double? precipitation = Blend(values, x => x.Precipitation, ScoreMetric.Rain, day, corrections, settings);
            double? probability = Blend(values, x => x.PrecipitationProbability, ScoreMetric.Rain, day, corrections, settings);
            result.Add(new Prediction { Time = hour.Key, LeadHours = leadHours, Temperature = temperature, FeelsLike = feelsLike, Humidity = humidity is double h ? Math.Clamp(h, 0, 100) : null, WindSpeed = windSpeed is double w ? Math.Max(0, w) : null, WindGust = windGust is double g ? Math.Max(0, g) : null, WindDirection = Direction(values, day, corrections, settings), Precipitation = precipitation is double p ? Math.Max(0, p) : null, PrecipitationProbability = probability is double c ? Math.Clamp(c, 0, 100) : null });
        }
        return result;
    }

    /// <summary>
    /// The weighted average of the sources' corrected values for one hour, or null if none have a value.
    /// </summary>
    /// <param name="values">Each source's hour.</param>
    /// <param name="selector">Picks the value from an hour.</param>
    /// <param name="metric">The metric the correction is looked up for.</param>
    /// <param name="day">How many days ahead.</param>
    /// <param name="corrections">Each source's correction by day ahead and metric.</param>
    /// <param name="settings">The scoring options.</param>
    private static double? Blend(List<(string Source, HourlyForecast Hour)> values, Func<HourlyForecast, double?> selector, ScoreMetric metric, int day, Dictionary<(string Source, int Day, ScoreMetric Metric), SourceCorrection> corrections, ScoringOptions settings)
    {
        List<(double Value, SourceCorrection? Correction)> list = [.. values.Select(x => (Value: selector(x.Hour), Correction: corrections.GetValueOrDefault((x.Source, day, metric)))).Where(x => x.Value is not null).Select(x => (x.Value!.Value - (x.Correction?.Bias ?? 0), x.Correction))];
        if (list.Count == 0)
        {
            return null;
        }
        List<double> weights = Weights([.. list.Select(x => x.Correction)], settings);
        return list.Select((x, i) => x.Value * weights[i]).Sum();
    }

    /// <summary>
    /// The weighted circular mean of the sources' wind directions for one hour, or null if none have one.
    /// </summary>
    /// <param name="values">Each source's hour.</param>
    /// <param name="day">How many days ahead.</param>
    /// <param name="corrections">Each source's correction by day ahead and metric.</param>
    /// <param name="settings">The scoring options.</param>
    private static double? Direction(List<(string Source, HourlyForecast Hour)> values, int day, Dictionary<(string Source, int Day, ScoreMetric Metric), SourceCorrection> corrections, ScoringOptions settings)
    {
        List<(double Degrees, SourceCorrection? Correction)> list = [.. values.Where(x => x.Hour.WindDirection is not null).Select(x => (x.Hour.WindDirection!.Value, corrections.GetValueOrDefault((x.Source, day, ScoreMetric.WindDirection))))];
        if (list.Count == 0)
        {
            return null;
        }
        List<double> weights = Weights([.. list.Select(x => x.Correction)], settings);
        double sin = list.Select((x, i) => Math.Sin(x.Degrees * Math.PI / 180) * weights[i]).Sum();
        double cos = list.Select((x, i) => Math.Cos(x.Degrees * Math.PI / 180) * weights[i]).Sum();
        double result = Math.Atan2(sin, cos) * 180 / Math.PI;
        return result < 0 ? result + 360 : result;
    }

    /// <summary>
    /// Each source's share of the average, adding up to 1: part equal share, part by accuracy.
    /// </summary>
    /// <param name="corrections">Each source's correction, or null if it doesn't have enough history.</param>
    /// <param name="settings">The scoring options.</param>
    private static List<double> Weights(List<SourceCorrection?> corrections, ScoringOptions settings)
    {
        // Sources without enough history get the average weight, so they're neither trusted nor ignored.
        List<double> known = [.. corrections.Where(x => x is not null).Select(x => x!.Weight)];
        double fallback = known.Count > 0 ? known.Average() : 1;
        List<double> raw = [.. corrections.Select(x => x?.Weight ?? fallback)];
        double total = raw.Sum();
        double equal = Math.Clamp(settings.EqualWeightShare, 0, 1);
        return [.. raw.Select(x => equal / raw.Count + (1 - equal) * x / total)];
    }
}