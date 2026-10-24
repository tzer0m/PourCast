namespace PourCast.Providers.OpenMeteoEnsemble;

/// <summary>
/// Configuration for the Open-Meteo ensemble provider.
/// </summary>
public class OpenMeteoEnsembleOptions : ProviderOptions
{
    /// <summary>
    /// The ensemble model to request, e.g. gfs05 for the 35 day GFS ensemble.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// The number of days to forecast.
    /// </summary>
    public int ForecastDays { get; set; } = 35;
}