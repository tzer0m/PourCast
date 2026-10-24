using System.Text.Json.Serialization;

namespace PourCast.Providers.VisualCrossing.Models;

/// <summary>
/// A single forecast day from Visual Crossing.
/// </summary>
public class VisualCrossingDay
{
    /// <summary>
    /// The hours in the day.
    /// </summary>
    [JsonPropertyName("hours")]
    public List<VisualCrossingHour> Hours { get; set; } = [];
}