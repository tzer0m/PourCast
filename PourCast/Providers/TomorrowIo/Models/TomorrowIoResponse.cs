using System.Text.Json.Serialization;

namespace PourCast.Providers.TomorrowIo.Models;

/// <summary>
/// Response from the Tomorrow.io forecast API.
/// </summary>
public class TomorrowIoResponse
{
    /// <summary>
    /// The forecast timelines.
    /// </summary>
    [JsonPropertyName("timelines")]
    public TomorrowIoTimelines? Timelines { get; set; }
}