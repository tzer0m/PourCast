namespace PourCast.Providers.OpenMeteo;

/// <summary>
/// Configuration for the Open-Meteo provider.
/// </summary>
public class OpenMeteoOptions : ProviderOptions
{
    /// <summary>
    /// The number of days to forecast.
    /// </summary>
    public int ForecastDays { get; set; } = 8;
}