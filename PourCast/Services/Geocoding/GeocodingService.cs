using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Models;
using PourCast.Services.Geocoding.Models;
using System.Globalization;

namespace PourCast.Services.Geocoding;

/// <summary>
/// Searches for places by name using the Open-Meteo geocoding API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for the geocoding API.</param>
/// <param name="options">The geocoding options.</param>
/// <param name="cache">The memory cache.</param>
public class GeocodingService(HttpClient httpClient, IOptions<GeocodingOptions> options, IMemoryCache cache)
{
    /// <summary>
    /// Searches for places matching a name.
    /// </summary>
    /// <param name="query">The place name to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<List<Place>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        string trimmed = query.Trim();
        if (trimmed.Length < 2)
        {
            return [];
        }
        string key = $"geocoding:{trimmed.ToLowerInvariant()}";
        List<Place>? places = await cache.GetOrCreateAsync(key, async (ICacheEntry entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheMinutes);
            string url = string.Create(CultureInfo.InvariantCulture, $"v1/search?name={Uri.EscapeDataString(trimmed)}&count={options.Value.ResultCount}&language=en&format=json");
            GeocodingResponse? response = await httpClient.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);
            return response?.Results?.Select(x => new Place { Name = x.Name, Region = x.Admin2 ?? x.Admin1, Country = x.Country, Latitude = x.Latitude, Longitude = x.Longitude }).ToList() ?? [];
        });
        return places ?? [];
    }
}