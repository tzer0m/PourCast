using System.Text.Json.Serialization;

namespace PourCast.Services.Geocoding.Models;

/// <summary>
/// Response from the Open-Meteo geocoding search endpoint.
/// </summary>
public class GeocodingResponse
{
    /// <summary>
    /// The matching places, or null if there were none.
    /// </summary>
    [JsonPropertyName("results")]
    public List<GeocodingResult>? Results { get; set; }
}