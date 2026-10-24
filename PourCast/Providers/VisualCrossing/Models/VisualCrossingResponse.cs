using System.Text.Json.Serialization;

namespace PourCast.Providers.VisualCrossing.Models;

/// <summary>
/// Response from the Visual Crossing timeline API.
/// </summary>
public class VisualCrossingResponse
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
    /// The forecast days.
    /// </summary>
    [JsonPropertyName("days")]
    public List<VisualCrossingDay> Days { get; set; } = [];
}