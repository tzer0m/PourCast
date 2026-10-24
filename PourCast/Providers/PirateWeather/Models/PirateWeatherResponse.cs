using System.Text.Json.Serialization;

namespace PourCast.Providers.PirateWeather.Models;

/// <summary>
/// Response from the Pirate Weather forecast API.
/// </summary>
public class PirateWeatherResponse
{
    /// <summary>
    /// The latitude of the forecast point.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude of the forecast point.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>
    /// The hourly forecast.
    /// </summary>
    [JsonPropertyName("hourly")]
    public PirateWeatherBlock? Hourly { get; set; }
}