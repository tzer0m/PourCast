namespace PourCast.Providers.PirateWeather;

/// <summary>
/// Configuration for the Pirate Weather provider.
/// </summary>
public class PirateWeatherOptions : ProviderOptions
{
    /// <summary>
    /// The Pirate Weather API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}