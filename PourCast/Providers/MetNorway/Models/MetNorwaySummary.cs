using System.Text.Json.Serialization;

namespace PourCast.Providers.MetNorway.Models;

/// <summary>
/// Weather summary for a MET Norway period.
/// </summary>
public class MetNorwaySummary
{
    /// <summary>
    /// The weather symbol, e.g. lightrainshowers_day.
    /// </summary>
    [JsonPropertyName("symbol_code")]
    public string SymbolCode { get; set; } = string.Empty;
}