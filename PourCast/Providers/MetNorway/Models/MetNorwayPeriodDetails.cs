using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// Values for a MET Norway period.
/// </summary>
public class MetNorwayPeriodDetails
{
    /// <summary>
    /// Precipitation in mm.
    /// </summary>
    [JsonPropertyName("precipitation_amount")]
    public double? PrecipitationAmount { get; set; }

    /// <summary>
    /// Chance of precipitation in percent.
    /// </summary>
    [JsonPropertyName("probability_of_precipitation")]
    public double? ProbabilityOfPrecipitation { get; set; }
}