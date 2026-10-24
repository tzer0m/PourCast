using System.Text.Json.Serialization;

namespace PourCast.Providers.OpenWeatherMap.Models;

/// <summary>
/// Response from the OpenWeatherMap 5 day / 3 hour forecast API.
/// </summary>
public class OpenWeatherMapResponse
{
    /// <summary>
    /// The forecast steps, every 3 hours.
    /// </summary>
    [JsonPropertyName("list")]
    public List<OpenWeatherMapStep> Steps { get; set; } = [];
}