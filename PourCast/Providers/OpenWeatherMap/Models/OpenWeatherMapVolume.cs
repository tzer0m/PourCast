using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenWeatherMap.Models;

/// <summary>
/// A precipitation volume from OpenWeatherMap.
/// </summary>
public class OpenWeatherMapVolume
{
    /// <summary>
    /// Precipitation over the 3 hours in mm.
    /// </summary>
    [JsonPropertyName("3h")]
    public double? ThreeHours { get; set; }
}