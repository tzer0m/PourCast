using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PourCast.Models;
using PourCast.Services;

namespace PourCast.Pages;

/// <summary>
/// Page model for the home page.
/// </summary>
/// <param name="weatherService">The weather service.</param>
public class IndexModel(WeatherService weatherService) : PageModel
{
    /// <summary>
    /// Handles GET requests for the home page.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Returns the forecasts from all providers for a location as JSON.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JsonResult> OnGetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        List<ProviderResult> results = await weatherService.GetForecastsAsync(latitude, longitude, cancellationToken);
        return new JsonResult(results);
    }
}