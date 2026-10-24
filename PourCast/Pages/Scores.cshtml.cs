using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PourCast.Scoring;
using PourCast.Scoring.Models;

namespace PourCast.Pages;

/// <summary>
/// Page model for the forecast accuracy page.
/// </summary>
/// <param name="scoringService">The accuracy scoring service.</param>
/// <param name="options">The scoring options.</param>
public class ScoresModel(ScoringService scoringService, IOptions<ScoringOptions> options) : PageModel
{
    /// <summary>
    /// Every source's scores, best first.
    /// </summary>
    public List<SourceScore> Scores { get; private set; } = [];

    /// <summary>
    /// How many observed hours are in the scoring window.
    /// </summary>
    public int ObservedHours { get; private set; }

    /// <summary>
    /// The scoring options.
    /// </summary>
    public ScoringOptions Settings => options.Value;

    /// <summary>
    /// The metrics in the order they're shown.
    /// </summary>
    public IReadOnlyList<ScoreMetric> Metrics { get; } = Enum.GetValues<ScoreMetric>();

    /// <summary>
    /// Loads the scores.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Scores = await scoringService.GetScoresAsync(cancellationToken);
        ObservedHours = await scoringService.GetObservedHoursAsync(cancellationToken);
    }

    /// <summary>
    /// The display name of a metric.
    /// </summary>
    /// <param name="metric">The metric.</param>
    public static string MetricName(ScoreMetric metric)
    {
        return metric switch
        {
            ScoreMetric.Temperature => "Temperature",
            ScoreMetric.FeelsLike => "Feels Like",
            ScoreMetric.Humidity => "Humidity",
            ScoreMetric.WindSpeed => "Wind",
            ScoreMetric.WindGust => "Gusts",
            ScoreMetric.WindDirection => "Direction",
            ScoreMetric.Rain => "Rain",
            _ => metric.ToString()
        };
    }

    /// <summary>
    /// A metric's error in its own units, e.g. ±1.2°C, or the Brier score for rain.
    /// </summary>
    /// <param name="metric">The metric.</param>
    /// <param name="error">The average error.</param>
    public static string FormatError(ScoreMetric metric, double error)
    {
        return metric switch
        {
            ScoreMetric.Temperature or ScoreMetric.FeelsLike => $"±{error:0.0}°C",
            ScoreMetric.Humidity => $"±{error:0}%",
            ScoreMetric.WindSpeed or ScoreMetric.WindGust => $"±{error:0.0} mph",
            ScoreMetric.WindDirection => $"±{error:0}°",
            ScoreMetric.Rain => $"{error:0.00}",
            _ => $"{error:0.00}"
        };
    }
}