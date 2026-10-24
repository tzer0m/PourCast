using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Scoring.Data;
using PourCast.Services;

namespace PourCast.Scoring;

/// <summary>
/// Records every source's forecast, and the combined forecast, at each snapshot hour so they can be scored against observations.
/// A snapshot missed while the app was down is taken as soon as it starts, if the next snapshot hour hasn't arrived yet.
/// </summary>
/// <param name="scopeFactory">Creates a scope for each snapshot, for the weather service and database context.</param>
/// <param name="options">The scoring options.</param>
/// <param name="combiningOptions">How sources are combined, and whether feels like is calculated.</param>
/// <param name="logger">The logger.</param>
public class SnapshotService(IServiceScopeFactory scopeFactory, IOptions<ScoringOptions> options, IOptions<CombiningOptions> combiningOptions, ILogger<SnapshotService> logger) : BackgroundService
{
    /// <summary>
    /// Takes any snapshot that's due, then waits for the next snapshot hour, retrying sooner if one failed.
    /// </summary>
    /// <param name="stoppingToken">Signals when the app is stopping.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool done = true;
            try
            {
                await RecordIfDueAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                done = false;
                logger.LogWarning(exception, "Couldn't record a forecast snapshot");
            }
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan untilNext = NextSlot(now) - now;
            TimeSpan delay = done ? untilNext : TimeSpan.FromMinutes(Math.Min(options.Value.RetryMinutes, untilNext.TotalMinutes));
            await Task.Delay(delay, stoppingToken);
        }
    }

    /// <summary>
    /// Records a snapshot if none has been taken since the latest snapshot hour.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task RecordIfDueAsync(CancellationToken cancellationToken)
    {
        ScoringOptions settings = options.Value;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset slot = LatestSlot(now);
        using IServiceScope scope = scopeFactory.CreateScope();
        ScoringDbContext db = scope.ServiceProvider.GetRequiredService<ScoringDbContext>();
        if (await db.Snapshots.AnyAsync(x => x.Source == settings.CombinedSource && x.IssuedAt >= slot, cancellationToken))
        {
            return;
        }

        // Forecasts come through the normal cache and usage limits, so a snapshot may reuse a recent page load's calls.
        WeatherService weatherService = scope.ServiceProvider.GetRequiredService<WeatherService>();
        List<Forecast> forecasts = await weatherService.GetForecastsAsync(settings.Latitude, settings.Longitude, ForecastRange.Week, cancellationToken);
        if (forecasts.Count == 0)
        {
            throw new InvalidOperationException("No sources returned a forecast.");
        }
        DateTimeOffset issuedAt = HourStart(now);
        DateTimeOffset end = issuedAt.AddDays(settings.SnapshotDays);
        foreach (Forecast forecast in forecasts)
        {
            List<Prediction> predictions = [.. forecast.Hourly.Where(x => x.Member is null && x.Time >= issuedAt && x.Time < end).Select(x => new Prediction { Time = x.Time.ToUniversalTime(), LeadHours = LeadHours(issuedAt, x.Time), Temperature = x.Temperature, FeelsLike = combiningOptions.Value.CalculateFeelsLike ? FeelsLikeCalculator.Calculate(x.Temperature, x.WindSpeed, x.Humidity, combiningOptions.Value) : x.FeelsLike, Humidity = x.Humidity, WindSpeed = x.WindSpeed, WindGust = x.WindGust, WindDirection = x.WindDirection, Precipitation = x.Precipitation, PrecipitationProbability = x.PrecipitationProbability })];
            if (predictions.Count > 0)
            {
                db.Snapshots.Add(new Snapshot { Source = forecast.Source, IssuedAt = issuedAt, Predictions = predictions });
            }
        }

        // The combined forecast, as shown on the site, so PourCast itself is scored too.
        List<HourlySummary> combined = ForecastCombiner.Combine(forecasts, ColumnBuilder.Hourly(forecasts), combiningOptions.Value);
        List<Prediction> combinedPredictions = [.. combined.Where(x => x.Time >= issuedAt && x.Time < end && x.TemperatureAverage is not null).Select(x => new Prediction { Time = x.Time.ToUniversalTime(), LeadHours = LeadHours(issuedAt, x.Time), Temperature = x.TemperatureAverage, FeelsLike = x.FeelsLikeAverage, Humidity = x.HumidityAverage, WindSpeed = x.WindSpeedAverage, WindGust = x.WindGustAverage, WindDirection = x.WindDirectionAverage, Precipitation = x.PrecipitationAverage, PrecipitationProbability = x.PrecipitationProbabilityAverage })];
        db.Snapshots.Add(new Snapshot { Source = settings.CombinedSource, IssuedAt = issuedAt, Predictions = combinedPredictions });
        await db.SaveChangesAsync(cancellationToken);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Recorded forecast snapshot from {Count} sources", forecasts.Count);
        }
    }

    /// <summary>
    /// The most recent snapshot hour at or before a time.
    /// </summary>
    /// <param name="time">The time.</param>
    private DateTimeOffset LatestSlot(DateTimeOffset time)
    {
        DateTimeOffset today = new(time.Year, time.Month, time.Day, 0, 0, 0, TimeSpan.Zero);
        List<DateTimeOffset> slots = [.. options.Value.SnapshotHours.Select(x => today.AddDays(-1).AddHours(x)), .. options.Value.SnapshotHours.Select(x => today.AddHours(x))];
        return slots.Where(x => x <= time).Max();
    }

    /// <summary>
    /// The next snapshot hour after a time.
    /// </summary>
    /// <param name="time">The time.</param>
    private DateTimeOffset NextSlot(DateTimeOffset time)
    {
        DateTimeOffset today = new(time.Year, time.Month, time.Day, 0, 0, 0, TimeSpan.Zero);
        List<DateTimeOffset> slots = [.. options.Value.SnapshotHours.Select(x => today.AddHours(x)), .. options.Value.SnapshotHours.Select(x => today.AddDays(1).AddHours(x))];
        return slots.Where(x => x > time).Min();
    }

    /// <summary>
    /// The start of the hour a time falls in, in UTC.
    /// </summary>
    /// <param name="time">The time.</param>
    private static DateTimeOffset HourStart(DateTimeOffset time)
    {
        DateTimeOffset utc = time.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }

    /// <summary>
    /// Whole hours between a snapshot and a predicted hour.
    /// </summary>
    /// <param name="issuedAt">When the snapshot was taken.</param>
    /// <param name="time">The predicted hour.</param>
    private static int LeadHours(DateTimeOffset issuedAt, DateTimeOffset time)
    {
        return (int)Math.Round((time - issuedAt).TotalHours);
    }
}