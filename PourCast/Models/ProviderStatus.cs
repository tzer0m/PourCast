namespace PourCast.Models;

/// <summary>
/// How a provider was used for a report.
/// </summary>
public enum ProviderStatus
{
    /// <summary>
    /// The provider returned a forecast that was used.
    /// </summary>
    Used,

    /// <summary>
    /// The provider was asked but failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The provider isn't configured for this range, so wasn't asked.
    /// </summary>
    NotUsed
}