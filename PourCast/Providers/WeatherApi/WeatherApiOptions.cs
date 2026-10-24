namespace PourCast.Providers.WeatherApi;

/// <summary>
/// Configuration for the WeatherAPI.com provider.
/// </summary>
public class WeatherApiOptions : ProviderOptions
{
    /// <summary>
    /// The WeatherAPI.com API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The number of days to forecast; the free plan allows 3.
    /// </summary>
    public int ForecastDays { get; set; } = 3;
}