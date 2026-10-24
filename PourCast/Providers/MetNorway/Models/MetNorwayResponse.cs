using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// Response from the MET Norway locationforecast API.
/// </summary>
public class MetNorwayResponse
{
    /// <summary>
    /// The forecast properties.
    /// </summary>
    [JsonPropertyName("properties")]
    public MetNorwayProperties? Properties { get; set; }
}