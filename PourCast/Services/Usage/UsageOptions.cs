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
}