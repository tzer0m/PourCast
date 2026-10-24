using System.Text.Json;
using System.Text.Json.Serialization;

namespace PourCast.Scoring.Metar;

/// <summary>
/// A single METAR report from the aviationweather.gov data API.
/// </summary>
public class MetarReport
{
    /// <summary>
    /// When the observation was made, in Unix seconds.
    /// </summary>
    [JsonPropertyName("obsTime")]
    public long ObservationTime { get; set; }

    /// <summary>
    /// Temperature in °C.
    /// </summary>
    [JsonPropertyName("temp")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Dew point in °C.
    /// </summary>
    [JsonPropertyName("dewp")]
    public double? DewPoint { get; set; }

    /// <summary>
    /// Wind direction in degrees, or the string VRB when variable.
    /// </summary>
    [JsonPropertyName("wdir")]
    public JsonElement? WindDirection { get; set; }

    /// <summary>
    /// Wind speed in knots.
    /// </summary>
    [JsonPropertyName("wspd")]
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind gust speed in knots, if a gust was reported.
    /// </summary>
    [JsonPropertyName("wgst")]
    public double? WindGust { get; set; }

    /// <summary>
    /// Present weather codes, e.g. "-RA BR".
    /// </summary>
    [JsonPropertyName("wxString")]
    public string? Weather { get; set; }

    /// <summary>
    /// The raw METAR text, used for recent weather codes such as RERA.
    /// </summary>
    [JsonPropertyName("rawOb")]
    public string? Raw { get; set; }
}