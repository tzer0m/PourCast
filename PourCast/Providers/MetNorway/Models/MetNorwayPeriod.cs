using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// Values for a period following a MET Norway time step.
/// </summary>
public class MetNorwayPeriod
{
    /// <summary>
    /// The weather summary for the period.
    /// </summary>
    [JsonPropertyName("summary")]
    public MetNorwaySummary? Summary { get; set; }

    /// <summary>
    /// The values for the period.
    /// </summary>
    [JsonPropertyName("details")]
    public MetNorwayPeriodDetails? Details { get; set; }
}