using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// Forecast data for a MET Norway time step.
/// </summary>
public class MetNorwayData
{
    /// <summary>
    /// Values at the instant of the step.
    /// </summary>
    [JsonPropertyName("instant")]
    public MetNorwayInstant? Instant { get; set; }

    /// <summary>
    /// Values for the following hour, present only for hourly steps.
    /// </summary>
    [JsonPropertyName("next_1_hours")]
    public MetNorwayPeriod? NextOneHours { get; set; }
}