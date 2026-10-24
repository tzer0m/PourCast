using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PourCast.Models;
using PourCast.Scoring;
using PourCast.Services;
using PourCast.Services.Geocoding;

namespace PourCast.Pages;

/// <summary>
/// Page model for the home page.
/// </summary>
/// <param name="weatherService">The weather service.</param>
/// <param name="geocodingService">The place search service.</param>
/// <param name="scoringService">The accuracy scoring service, for each source's score.</param>
/// <param name="logger">The logger.</param>
public class IndexModel(WeatherService weatherService, GeocodingService geocodingService, ScoringService scoringService, ILogger<IndexModel> logger) : PageModel
{
    /// <summary>
    /// Handles GET requests for the home page.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Returns the combined weather report for a location as JSON.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="range">The forecast range, week by default.</param>
    /// <param name="timeZone">The place's IANA time zone, e.g. Europe/London. UTC if missing or unknown.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JsonResult> OnGetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken, ForecastRange range = ForecastRange.Week, string? timeZone = null)
    {
        TimeZoneInfo zone = !string.IsNullOrWhiteSpace(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out TimeZoneInfo? found) ? found! : TimeZoneInfo.Utc;
        WeatherReport report = await weatherService.GetReportAsync(latitude, longitude, range, zone, cancellationToken);

        // Scores are extra, so the forecast is still returned if the scoring database can't be reached.
        try
        {
            Dictionary<string, double?> scores = (await scoringService.GetScoresAsync(cancellationToken)).ToDictionary(x => x.Source, x => x.Overall);
            foreach (ProviderResult source in report.Sources)
            {
                source.Score = scores.GetValueOrDefault(source.Source);
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Couldn't get accuracy scores");
        }
        return new JsonResult(report);
    }

    /// <summary>
    /// Returns places matching a search as JSON.
    /// </summary>
    /// <param name="query">The place name to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JsonResult> OnGetSearchAsync(string query, CancellationToken cancellationToken)
    {
        List<Place> places = await geocodingService.SearchAsync(query ?? string.Empty, cancellationToken);
        return new JsonResult(places);
    }
}