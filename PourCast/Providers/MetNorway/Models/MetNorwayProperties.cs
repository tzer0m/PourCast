using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// Properties of a MET Norway forecast.
/// </summary>
public class MetNorwayProperties
{
    /// <summary>
    /// The forecast time steps.
    /// </summary>
    [JsonPropertyName("timeseries")]
    public List<MetNorwayTimeStep> Timeseries { get; set; } = [];
}