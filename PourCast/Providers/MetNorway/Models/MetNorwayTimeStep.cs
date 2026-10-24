using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// A single time step in a MET Norway forecast.
/// </summary>
public class MetNorwayTimeStep
{
    /// <summary>
    /// The UTC time of the step.
    /// </summary>
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// The forecast data for the step.
    /// </summary>
    [JsonPropertyName("data")]
    public MetNorwayData? Data { get; set; }
}