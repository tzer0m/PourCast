namespace PourCast.Providers.MetOffice;

/// <summary>
/// Configuration for the Met Office DataHub provider.
/// </summary>
public class MetOfficeOptions : ProviderOptions
{
    /// <summary>
    /// The DataHub API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}