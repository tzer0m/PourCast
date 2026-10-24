using System.Net;

namespace PourCast.Services.Usage;

/// <summary>
/// HTTP handler that counts every outgoing call, refuses calls past the daily limit or during a cooldown, and starts a cooldown on "too many requests".
/// </summary>
/// <param name="tracker">The usage tracker.</param>
/// <param name="key">The usage key to count against.</param>
/// <param name="limit">The daily limit, or null for no limit.</param>
public class UsageHandler(UsageTracker tracker, string key, int? limit) : DelegatingHandler
{
    /// <summary>
    /// Checks the cooldown and limit, sends the call, and starts a cooldown if the provider rate limits us.
    /// </summary>
    /// <param name="request">The outgoing request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        DateTimeOffset? cooldownEnd = tracker.GetCooldownEnd(key);
        if (cooldownEnd is not null)
        {
            throw new RateLimitedException(key, cooldownEnd.Value);
        }
        if (!tracker.TryRecord(key, limit))
        {
            throw new DailyLimitExceededException(key, limit);
        }
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // Retry-After can be a number of seconds or a date.
            TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta ?? response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow;
            DateTimeOffset end = tracker.StartCooldown(key, retryAfter);
            response.Dispose();
            throw new RateLimitedException(key, end);
        }
        return response;
    }
}