namespace PourCast.Providers.MetNorway;

/// <summary>
/// Configuration for the MET Norway provider.
/// </summary>
public class MetNorwayOptions : ProviderOptions
{
    /// <summary>
    /// The User-Agent sent with each request. MET Norway requires one that identifies the app and a contact.
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;
}