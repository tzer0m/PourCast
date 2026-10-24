namespace PourCast.Providers.OpenMeteo;

/// <summary>
/// Configuration for the Open-Meteo provider.
/// </summary>
public class OpenMeteoOptions : ProviderOptions
{
    /// <summary>
    /// The name shown for this source, e.g. Open-Meteo UKMO.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The weather model to request, e.g. ukmo_seamless. Empty for Open-Meteo's best match blend.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// The number of days to forecast.
    /// </summary>
    public int ForecastDays { get; set; } = 8;
}