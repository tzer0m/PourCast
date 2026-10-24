namespace PourCast.Services.Usage;

/// <summary>
/// Thrown when a provider has told us to slow down (HTTP 429) and its cooldown hasn't finished.
/// </summary>
/// <param name="key">The usage key that is cooling down.</param>
/// <param name="until">When calls can be made again.</param>
public class RateLimitedException(string key, DateTimeOffset until) : Exception($"Rate limited by {key} until {until:HH:mm} UTC.")
{
    /// <summary>
    /// When calls can be made again.
    /// </summary>
    public DateTimeOffset Until { get; } = until;
}