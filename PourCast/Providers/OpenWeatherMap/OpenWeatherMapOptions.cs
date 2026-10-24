namespace PourCast.Providers.OpenWeatherMap;

/// <summary>
/// Configuration for the OpenWeatherMap provider.
/// </summary>
public class OpenWeatherMapOptions : ProviderOptions
{
    /// <summary>
    /// The OpenWeatherMap API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}