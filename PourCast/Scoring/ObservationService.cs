using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PourCast.Scoring.Data;
using PourCast.Scoring.Metar;
using PourCast.Scoring.Metar.Models;
using PourCast.Services;

namespace PourCast.Scoring;

/// <summary>
/// Records the METAR station's observations every half hour, refetching recent hours so any gaps are filled in.
/// </summary>
/// <param name="scopeFactory">Creates a scope for each run, for the database context.</param>
/// <param name="metarClient">The METAR client.</param>
/// <param name="options">The scoring options.</param>
/// <param name="combiningOptions">The feels like thresholds.</param>
/// <param name="logger">The logger.</param>
public class ObservationService(IServiceScopeFactory scopeFactory, MetarClient metarClient, IOptions<ScoringOptions> options, IOptions<CombiningOptions> combiningOptions, ILogger<ObservationService> logger) : BackgroundService
{
    /// <summary>
    /// Records observations straight away, then every half hour.
    /// </summary>
    /// <param name="stoppingToken">Signals when the app is stopping.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(30));
        do
        {
            try
            {
                await RecordAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Couldn't record observations for {Station}", options.Value.Station);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Fetches recent reports and adds or updates an observation for each hour.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task RecordAsync(CancellationToken cancellationToken)
    {
        string station = options.Value.Station;
        List<MetarReport> reports = await metarClient.GetReportsAsync(cancellationToken);
        List<Observation> observations = ObservationBuilder.Build(station, reports, combiningOptions.Value);
        if (observations.Count == 0)
        {
            return;
        }
        using IServiceScope scope = scopeFactory.CreateScope();
        ScoringDbContext db = scope.ServiceProvider.GetRequiredService<ScoringDbContext>();
        DateTimeOffset from = observations.Min(x => x.Time);
        Dictionary<DateTimeOffset, Observation> existing = await db.Observations.Where(x => x.Station == station && x.Time >= from).ToDictionaryAsync(x => x.Time, cancellationToken);
        foreach (Observation observation in observations)
        {
            if (existing.TryGetValue(observation.Time, out Observation? saved))
            {
                saved.Temperature = observation.Temperature;
                saved.Humidity = observation.Humidity;
                saved.FeelsLike = observation.FeelsLike;
                saved.WindSpeed = observation.WindSpeed;
                saved.WindGust = observation.WindGust;
                saved.WindDirection = observation.WindDirection;
                saved.Raining = observation.Raining;
            }
            else
            {
                db.Observations.Add(observation);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}