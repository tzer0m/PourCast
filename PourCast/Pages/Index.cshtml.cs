using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PourCast.Models;
using PourCast.Services;
using PourCast.Services.Geocoding;

namespace PourCast.Pages;

/// <summary>
/// Page model for the home page.
/// </summary>
/// <param name="weatherService">The weather service.</param>
/// <param name="geocodingService">The place search service.</param>
public class IndexModel(WeatherService weatherService, GeocodingService geocodingService) : PageModel
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
    /// <param name="timeZone">The viewer's IANA time zone, e.g. Europe/London. UTC if missing or unknown.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JsonResult> OnGetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken, ForecastRange range = ForecastRange.Week, string? timeZone = null)
    {
        TimeZoneInfo zone = !string.IsNullOrWhiteSpace(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out TimeZoneInfo? found) ? found! : TimeZoneInfo.Utc;
        WeatherReport report = await weatherService.GetReportAsync(latitude, longitude, range, zone, cancellationToken);
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