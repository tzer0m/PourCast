using System.Text.Json.Serialization;

namespace PourCast.Providers.TomorrowIo.Models;

/// <summary>
/// Forecast timelines from Tomorrow.io.
/// </summary>
public class TomorrowIoTimelines
{
    /// <summary>
    /// The hourly forecast.
    /// </summary>
    [JsonPropertyName("hourly")]
    public List<TomorrowIoInterval> Hourly { get; set; } = [];
}