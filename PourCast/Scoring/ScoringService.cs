using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Scoring.Data;
using PourCast.Scoring.Models;

namespace PourCast.Scoring;

/// <summary>
/// Scores each source's recorded predictions against the observations, by metric and day ahead.
/// </summary>
/// <param name="db">The scoring database context.</param>
/// <param name="options">The scoring options.</param>
/// <param name="cache">The memory cache, so scores are only worked out every so often.</param>
public class ScoringService(ScoringDbContext db, IOptions<ScoringOptions> options, IMemoryCache cache)
{
    /// <summary>
    /// Gets every source's scores over the scoring window, best first. Sources without enough data come last with no overall score.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<ScoreBoard> GetScoresAsync(CancellationToken cancellationToken)
    {
        ScoreBoard? board = await cache.GetOrCreateAsync<ScoreBoard>("scores", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.ScoreCacheMinutes);
            List<MatchedHour> matches = await GetMatchesAsync(cancellationToken);
            List<SourceScore> sources = [.. matches.GroupBy(x => x.Source).Select(x => ScoreSource(x.Key, [.. x]))];
            int commonDays = CompareSources(sources);
            return new ScoreBoard { Sources = [.. sources.OrderByDescending(x => x.Overall.HasValue).ThenByDescending(x => x.Overall).ThenBy(x => x.Source)], CommonDays = commonDays };
        });
        return board ?? new ScoreBoard();
    }

    /// <summary>
    /// Works out each source's average error and weight for each day ahead and metric, for the weighted forecast.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Dictionary<(string Source, int Day, ScoreMetric Metric), SourceCorrection>> GetCorrectionsAsync(CancellationToken cancellationToken)
    {
        ScoringOptions settings = options.Value;
        List<MatchedHour> matches = await GetMatchesAsync(cancellationToken);
        Dictionary<(string Source, int Day, ScoreMetric Metric), SourceCorrection> corrections = [];

        // The combined forecasts are left out, so they're only built from the real sources.
        foreach (IGrouping<(string Source, int Day), MatchedHour> group in matches.Where(x => x.Source != settings.CombinedSource && x.Source != settings.WeightedSource).GroupBy(x => (x.Source, Day: x.LeadHours / 24 + 1)))
        {
            AddCorrection(corrections, group.Key, ScoreMetric.Temperature, [.. group.Where(x => x.PredictedTemperature is not null && x.ObservedTemperature is not null).Select(x => x.PredictedTemperature!.Value - x.ObservedTemperature!.Value)], true);
            AddCorrection(corrections, group.Key, ScoreMetric.FeelsLike, [.. group.Where(x => x.PredictedFeelsLike is not null && x.ObservedFeelsLike is not null).Select(x => x.PredictedFeelsLike!.Value - x.ObservedFeelsLike!.Value)], true);
            AddCorrection(corrections, group.Key, ScoreMetric.Humidity, [.. group.Where(x => x.PredictedHumidity is not null && x.ObservedHumidity is not null).Select(x => x.PredictedHumidity!.Value - x.ObservedHumidity!.Value)], true);
            AddCorrection(corrections, group.Key, ScoreMetric.WindSpeed, [.. group.Where(x => x.PredictedWindSpeed is not null && x.ObservedWindSpeed is not null).Select(x => x.PredictedWindSpeed!.Value - x.ObservedWindSpeed!.Value)], true);
            AddCorrection(corrections, group.Key, ScoreMetric.WindGust, [.. group.Where(x => x.PredictedWindGust is not null && x.ObservedWindGust is not null).Select(x => x.PredictedWindGust!.Value - x.ObservedWindGust!.Value)], true);

            // Direction and rain are only weighted, not corrected: a direction offset is rarely consistent, and rain chances need calibrating rather than shifting.
            AddCorrection(corrections, group.Key, ScoreMetric.WindDirection, [.. group.Where(x => x.PredictedWindDirection is not null && x.ObservedWindDirection is not null && x.ObservedWindSpeed >= settings.DirectionMinimumWind).Select(x => AngleBetween(x.PredictedWindDirection!.Value, x.ObservedWindDirection!.Value))], false);
            AddCorrection(corrections, group.Key, ScoreMetric.Rain, [.. group.Select(x => (Chance: RainChance(x), x.ObservedRaining)).Where(x => x.Chance is not null).Select(x => x.Chance!.Value - (x.ObservedRaining ? 1 : 0))], false);
        }
        return corrections;
    }

    /// <summary>
    /// Counts the observed hours in the scoring window.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<int> GetObservedHoursAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-options.Value.WindowDays);
        return await db.Observations.CountAsync(x => x.Station == options.Value.Station && x.Time >= start, cancellationToken);
    }

    /// <summary>
    /// Pairs every prediction in the scoring window with the observation for the same hour.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<List<MatchedHour>> GetMatchesAsync(CancellationToken cancellationToken)
    {
        ScoringOptions settings = options.Value;
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-settings.WindowDays);
        IQueryable<MatchedHour> query = from prediction in db.Predictions join observation in db.Observations on prediction.Time equals observation.Time where observation.Station == settings.Station && prediction.Time >= start && prediction.LeadHours >= 0 && prediction.LeadHours < settings.SnapshotDays * 24 select new MatchedHour { Source = prediction.Snapshot!.Source, LeadHours = prediction.LeadHours, PredictedTemperature = prediction.Temperature, PredictedFeelsLike = prediction.FeelsLike, PredictedHumidity = prediction.Humidity, PredictedWindSpeed = prediction.WindSpeed, PredictedWindGust = prediction.WindGust, PredictedWindDirection = prediction.WindDirection, PredictedPrecipitation = prediction.Precipitation, PredictedPrecipitationProbability = prediction.PrecipitationProbability, ObservedTemperature = observation.Temperature, ObservedFeelsLike = observation.FeelsLike, ObservedHumidity = observation.Humidity, ObservedWindSpeed = observation.WindSpeed, ObservedWindGust = observation.WindGust, ObservedWindDirection = observation.WindDirection, ObservedRaining = observation.Raining };
        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Scores one source for each day ahead that had enough hours.
    /// </summary>
    /// <param name="source">The forecast source.</param>
    /// <param name="matches">The source's matched hours.</param>
    private SourceScore ScoreSource(string source, List<MatchedHour> matches)
    {
        List<DayScore> days = [.. matches.GroupBy(x => x.LeadHours / 24 + 1).OrderBy(x => x.Key).Select(x => ScoreDay(x.Key, [.. x])).Where(x => x is not null).Select(x => x!)];
        return new SourceScore { Source = source, Days = days };
    }

    /// <summary>
    /// Works out each source's overall score against the others on the same days and metrics, and returns how many days every source covers.
    /// </summary>
    /// <param name="sources">Every source's day scores.</param>
    private static int CompareSources(List<SourceScore> sources)
    {
        // Later days and harder metrics score lower for everyone, so each source is judged against the average of the
        // sources that also have that day and metric. Otherwise sources with short forecasts, or without wind direction
        // or gusts, would look better just for skipping the hard parts.
        Dictionary<(int Day, ScoreMetric Metric), double> field = sources.SelectMany(x => x.Days).SelectMany(x => x.Metrics.Select(y => (x.Day, Metric: y.Key, y.Value.Score))).GroupBy(x => (x.Day, x.Metric)).Where(x => x.Count() >= 2).ToDictionary(x => x.Key, x => x.Average(y => y.Score));
        double? baseline = field.Count > 0 ? field.GroupBy(x => x.Key.Day).Average(x => x.Average(y => y.Value)) : null;
        foreach (SourceScore source in sources)
        {
            List<double> skills = [.. source.Days.Select(x => x.Metrics.Where(y => field.ContainsKey((x.Day, y.Key))).Select(y => y.Value.Score - field[(x.Day, y.Key)])).Where(x => x.Any()).Select(x => x.Average())];
            source.Skill = skills.Count > 0 ? skills.Average() : null;

            // The overall score is the field's average over every day and metric, plus how far ahead or behind the source was.
            source.Overall = baseline is double average && source.Skill is double skill ? Math.Clamp(average + skill, 0, 100) : source.Days.Count > 0 ? source.Days.Average(x => x.Overall) : null;
        }

        // The plain average over the days every source has, as a like for like check on the overall score.
        int commonDays = sources.Where(x => x.Days.Count > 0).Select(CoveredDays).DefaultIfEmpty(0).Min();
        foreach (SourceScore source in sources.Where(x => commonDays > 0 && x.Days.Count > 0))
        {
            source.CommonOverall = source.Days.Where(x => x.Day <= commonDays).Average(x => x.Overall);
        }
        return commonDays;
    }

    /// <summary>
    /// How many days in a row, from day 1, a source has scores for.
    /// </summary>
    /// <param name="source">The source's scores.</param>
    private static int CoveredDays(SourceScore source)
    {
        int days = 0;
        while (source.Days.Any(x => x.Day == days + 1))
        {
            days++;
        }
        return days;
    }

    /// <summary>
    /// Scores one source at one day ahead, or null if there weren't enough hours for temperature.
    /// </summary>
    /// <param name="day">How many days ahead.</param>
    /// <param name="matches">The source's matched hours for that day.</param>
    private DayScore? ScoreDay(int day, List<MatchedHour> matches)
    {
        ScoringOptions settings = options.Value;
        Dictionary<ScoreMetric, MetricScore> metrics = [];
        AddMetric(metrics, ScoreMetric.Temperature, [.. matches.Where(x => x.PredictedTemperature is not null && x.ObservedTemperature is not null).Select(x => Math.Abs(x.PredictedTemperature!.Value - x.ObservedTemperature!.Value))]);
        AddMetric(metrics, ScoreMetric.FeelsLike, [.. matches.Where(x => x.PredictedFeelsLike is not null && x.ObservedFeelsLike is not null).Select(x => Math.Abs(x.PredictedFeelsLike!.Value - x.ObservedFeelsLike!.Value))]);
        AddMetric(metrics, ScoreMetric.Humidity, [.. matches.Where(x => x.PredictedHumidity is not null && x.ObservedHumidity is not null).Select(x => Math.Abs(x.PredictedHumidity!.Value - x.ObservedHumidity!.Value))]);
        AddMetric(metrics, ScoreMetric.WindSpeed, [.. matches.Where(x => x.PredictedWindSpeed is not null && x.ObservedWindSpeed is not null).Select(x => Math.Abs(x.PredictedWindSpeed!.Value - x.ObservedWindSpeed!.Value))]);
        AddMetric(metrics, ScoreMetric.WindGust, [.. matches.Where(x => x.PredictedWindGust is not null && x.ObservedWindGust is not null).Select(x => Math.Abs(x.PredictedWindGust!.Value - x.ObservedWindGust!.Value))]);
        AddMetric(metrics, ScoreMetric.WindDirection, [.. matches.Where(x => x.PredictedWindDirection is not null && x.ObservedWindDirection is not null && x.ObservedWindSpeed >= settings.DirectionMinimumWind).Select(x => AngleBetween(x.PredictedWindDirection!.Value, x.ObservedWindDirection!.Value))]);

        // Rain uses the chance of rain where given, otherwise whether any rain was predicted.
        List<(double Chance, bool Raining)> rain = [.. matches.Select(x => (Chance: RainChance(x), x.ObservedRaining)).Where(x => x.Chance is not null).Select(x => (x.Chance!.Value, x.ObservedRaining))];
        AddMetric(metrics, ScoreMetric.Rain, [.. rain.Select(x => Math.Pow(x.Chance - (x.Raining ? 1 : 0), 2))]);
        // A day is only scored once temperature has enough hours, so a day isn't judged on rain alone while the
        // other metrics are still collecting, e.g. sources with 3 hourly readings but hourly rain.
        if (!metrics.ContainsKey(ScoreMetric.Temperature))
        {
            return null;
        }
        double? hitRate = rain.Count >= settings.MinimumMatches ? rain.Count(x => (x.Chance >= 0.5) == x.Raining) * 100.0 / rain.Count : null;
        return new DayScore { Day = day, Overall = metrics.Values.Average(x => x.Score), Matches = matches.Count, RainHitRate = hitRate, Metrics = metrics };
    }

    /// <summary>
    /// Adds a source's correction for a day ahead and metric, if there are enough hours to judge it.
    /// </summary>
    /// <param name="corrections">The corrections to add to.</param>
    /// <param name="key">The source and day ahead.</param>
    /// <param name="metric">The metric.</param>
    /// <param name="errors">The predicted minus observed value for each hour.</param>
    /// <param name="correctBias">Whether the average error is taken off, or only used for the weight.</param>
    private void AddCorrection(Dictionary<(string Source, int Day, ScoreMetric Metric), SourceCorrection> corrections, (string Source, int Day) key, ScoreMetric metric, List<double> errors, bool correctBias)
    {
        ScoringOptions settings = options.Value;
        if (errors.Count < settings.MinimumMatches)
        {
            return;
        }

        // The average error is scaled down while there are few hours, so it grows into the full correction as evidence builds.
        double bias = correctBias ? errors.Average() * errors.Count / (errors.Count + settings.BiasShrinkHours) : 0;
        double meanSquare = errors.Average(x => Math.Pow(x - bias, 2));
        corrections[(key.Source, key.Day, metric)] = new SourceCorrection { Bias = bias, Weight = 1 / Math.Max(meanSquare, 0.0001), Matches = errors.Count };
    }

    /// <summary>
    /// Adds a metric's average error and score, if there are enough hours to judge it.
    /// </summary>
    /// <param name="metrics">The day's metrics to add to.</param>
    /// <param name="metric">The metric.</param>
    /// <param name="errors">The error for each hour, or the squared error for rain.</param>
    private void AddMetric(Dictionary<ScoreMetric, MetricScore> metrics, ScoreMetric metric, List<double> errors)
    {
        ScoringOptions settings = options.Value;
        if (errors.Count < settings.MinimumMatches || !settings.Tolerances.TryGetValue(metric, out double tolerance) || tolerance <= 0)
        {
            return;
        }
        double error = errors.Average();
        metrics[metric] = new MetricScore { Error = error, Score = Math.Max(0, 100 * (1 - error / tolerance)), Matches = errors.Count };
    }

    /// <summary>
    /// The predicted chance of rain from 0 to 1: the chance of rain if given, otherwise 1 if enough rain was predicted and 0 if not.
    /// </summary>
    /// <param name="match">The matched hour.</param>
    private double? RainChance(MatchedHour match)
    {
        if (match.PredictedPrecipitationProbability is not null)
        {
            return Math.Clamp(match.PredictedPrecipitationProbability.Value / 100, 0, 1);
        }
        if (match.PredictedPrecipitation is not null)
        {
            return match.PredictedPrecipitation.Value >= options.Value.RainThreshold ? 1 : 0;
        }
        return null;
    }

    /// <summary>
    /// The smallest angle between two directions, from 0 to 180 degrees.
    /// </summary>
    /// <param name="first">The first direction in degrees.</param>
    /// <param name="second">The second direction in degrees.</param>
    private static double AngleBetween(double first, double second)
    {
        double difference = Math.Abs(first - second) % 360;
        return difference > 180 ? 360 - difference : difference;
    }
}