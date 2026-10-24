using System.Text.Json.Serialization;

namespace PourCast.Providers.WeatherApi.Models;

/// <summary>
/// The location WeatherAPI.com resolved the request to.
/// </summary>
public class WeatherApiLocation
{
    /// <summary>
    /// The place name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The latitude.
    /// </summary>
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    /// <summary>
    /// The longitude.
    /// </summary>
    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}