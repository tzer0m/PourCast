namespace PourCast.Services.Usage;

/// <summary>
/// Thrown when a call would go over a provider's daily limit.
/// </summary>
/// <param name="key">The usage key that hit its limit.</param>
/// <param name="limit">The daily limit.</param>
public class DailyLimitExceededException(string key, int? limit) : Exception($"Daily limit of {limit} calls reached for {key}.")
{
}