using System.Text.Json.Serialization;

namespace PourCast.Services.Geocoding.Models;

/// <summary>
/// A single place from the Open-Meteo geocoding search.
/// </summary>
public class GeocodingResult
{
    /// <summary>
    /// The name of the place.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The latitude of the place.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude of the place.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>
    /// The country name.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// The first level administrative area, e.g. England or a US state.
    /// </summary>
    [JsonPropertyName("admin1")]
    public string? Admin1 { get; set; }

    /// <summary>
    /// The second level administrative area, e.g. a county.
    /// </summary>
    [JsonPropertyName("admin2")]
    public string? Admin2 { get; set; }
}