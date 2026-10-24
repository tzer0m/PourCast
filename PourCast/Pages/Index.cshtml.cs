using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PourCast.Models;
using PourCast.Providers;

namespace PourCast.Pages;

/// <summary>
/// Page model for the home page.
/// </summary>
/// <param name="provider">The weather provider.</param>
public class IndexModel(IWeatherProvider provider) : PageModel
{
    /// <summary>
    /// Handles GET requests for the home page.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Returns the forecast for a location as JSON.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JsonResult> OnGetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        Forecast forecast = await provider.GetForecastAsync(latitude, longitude, cancellationToken);
        return new JsonResult(forecast);
    }
}