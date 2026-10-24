namespace PourCast.Services.Usage;

/// <summary>
/// Configuration for API usage tracking.
/// </summary>
public class UsageOptions
{
    /// <summary>
    /// The file today's call counts are saved to, relative to the app's content root unless absolute.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// How long to wait after a "too many requests" response that doesn't say how long, in minutes.
    /// </summary>
    public int DefaultCooldownMinutes { get; set; } = 15;

    /// <summary>
    /// How long to remember a failed forecast request before trying the provider again, in minutes.
    /// </summary>
    public int FailureCacheMinutes { get; set; } = 2;
}