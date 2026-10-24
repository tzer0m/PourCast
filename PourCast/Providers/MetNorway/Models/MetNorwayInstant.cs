using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// Instant values for a MET Norway time step.
/// </summary>
public class MetNorwayInstant
{
    /// <summary>
    /// The instant values.
    /// </summary>
    [JsonPropertyName("details")]
    public MetNorwayInstantDetails? Details { get; set; }
}