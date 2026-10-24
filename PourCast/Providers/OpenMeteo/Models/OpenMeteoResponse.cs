using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenMeteo.Models;

/// <summary>
/// Response from the Open-Meteo forecast endpoint.
/// </summary>
public class OpenMeteoResponse
{
    /// <summary>
    /// The latitude of the grid cell used.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude of the grid cell used.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>
    /// The location's offset from UTC in seconds.
    /// </summary>
    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    /// <summary>
    /// The current conditions.
    /// </summary>
    [JsonPropertyName("current")]
    public OpenMeteoCurrent? Current { get; set; }

    /// <summary>
    /// The hourly forecast arrays.
    /// </summary>
    [JsonPropertyName("hourly")]
    public OpenMeteoHourly? Hourly { get; set; }

    /// <summary>
    /// The daily forecast arrays.
    /// </summary>
    [JsonPropertyName("daily")]
    public OpenMeteoDaily? Daily { get; set; }
}