using PourCast.Models;
using PourCast.Services;

namespace PourCast.Endpoints;

/// <summary>
/// The endpoint that feeds the Tidgets home screen widget.
/// </summary>
public static class WidgetEndpoint
{
    /// <summary>
    /// Returns the widget summary for a place as JSON.
    /// </summary>
    /// <param name="widgetService">The widget service.</param>
    /// <param name="latitude">The latitude of the place.</param>
    /// <param name="longitude">The longitude of the place.</param>
    /// <param name="name">The place name to show, optional.</param>
    /// <param name="timeZone">The place's IANA time zone, optional, UTC if missing.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<WidgetResponse> GetAsync(WidgetService widgetService, double latitude, double longitude, string? name, string? timeZone, CancellationToken cancellationToken)
    {
        return await widgetService.GetAsync(latitude, longitude, name, timeZone, cancellationToken);
    }
}