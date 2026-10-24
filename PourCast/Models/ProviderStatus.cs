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
    NotUsed,

    /// <summary>
    /// The provider was skipped because its daily limit has been reached.
    /// </summary>
    LimitReached,

    /// <summary>
    /// The provider asked us to slow down, so it's paused until its cooldown ends.
    /// </summary>
    RateLimited
}