using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PourCast.Models;
using PourCast.Services;

namespace PourCast.Pages;

/// <summary>
/// Page model for the source comparison page, which shows each source's values side by side.
/// </summary>
/// <param name="weatherService">The weather service.</param>
public class SourcesModel(WeatherService weatherService) : PageModel
{
    /// <summary>
    /// Handles GET requests for the source comparison page.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Returns each source's mapped hours for the week range as JSON.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JsonResult> OnGetDataAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        List<SourceForecast> sources = await weatherService.GetSourceForecastsAsync(latitude, longitude, ForecastRange.Week, cancellationToken);
        return new JsonResult(sources.Where(x => x.Status != ProviderStatus.NotUsed));
    }
}