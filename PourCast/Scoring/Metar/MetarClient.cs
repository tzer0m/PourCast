using Microsoft.Extensions.Options;
using PourCast.Scoring.Metar.Models;
using System.Globalization;
using System.Net;

namespace PourCast.Scoring.Metar;

/// <summary>
/// Fetches recent METAR reports from the aviationweather.gov data API.
/// </summary>
/// <param name="httpClient">The HTTP client configured for aviationweather.gov.</param>
/// <param name="options">The scoring options.</param>
public class MetarClient(HttpClient httpClient, IOptions<ScoringOptions> options)
{
    /// <summary>
    /// Gets the configured station's reports for the configured number of past hours, oldest first.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<List<MetarReport>> GetReportsAsync(CancellationToken cancellationToken)
    {
        string url = string.Create(CultureInfo.InvariantCulture, $"api/data/metar?ids={Uri.EscapeDataString(options.Value.Station)}&format=json&hours={options.Value.MetarHours}");
        using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        // No content means no reports in the period.
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return [];
        }
        List<MetarReport>? reports = await response.Content.ReadFromJsonAsync<List<MetarReport>>(cancellationToken);
        return reports?.OrderBy(x => x.ObservationTime).ToList() ?? [];
    }
}