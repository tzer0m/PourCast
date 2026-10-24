namespace PourCast.Providers.TomorrowIo;

/// <summary>
/// Configuration for the Tomorrow.io provider.
/// </summary>
public class TomorrowIoOptions : ProviderOptions
{
    /// <summary>
    /// The Tomorrow.io API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}