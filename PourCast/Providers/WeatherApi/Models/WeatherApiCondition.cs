using System.Text.Json.Serialization;

namespace PourCast.Providers.WeatherApi.Models;

/// <summary>
/// A weather condition from WeatherAPI.com.
/// </summary>
public class WeatherApiCondition
{
    /// <summary>
    /// The condition code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// The condition description.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}