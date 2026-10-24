namespace PourCast.Services.Usage;

/// <summary>
/// HTTP handler that counts every outgoing call and refuses calls once the daily limit is reached.
/// </summary>
/// <param name="tracker">The usage tracker.</param>
/// <param name="key">The usage key to count against.</param>
/// <param name="limit">The daily limit, or null for no limit.</param>
public class UsageHandler(UsageTracker tracker, string key, int? limit) : DelegatingHandler
{
    /// <summary>
    /// Counts the call, or throws if the daily limit has been reached.
    /// </summary>
    /// <param name="request">The outgoing request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!tracker.TryRecord(key, limit))
        {
            throw new DailyLimitExceededException(key, limit);
        }
        return base.SendAsync(request, cancellationToken);
    }
}