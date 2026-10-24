using System.Text.Json.Serialization;

namespace PourCast.Providers.TomorrowIo.Models;

/// <summary>
/// A single interval in a Tomorrow.io timeline.
/// </summary>
public class TomorrowIoInterval
{
    /// <summary>
    /// The UTC start of the interval.
    /// </summary>
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// The forecast values.
    /// </summary>
    [JsonPropertyName("values")]
    public TomorrowIoValues? Values { get; set; }
}