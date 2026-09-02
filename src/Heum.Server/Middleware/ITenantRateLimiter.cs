namespace Heum.Server.Middleware;

public interface ITenantRateLimiter
{
    /// <summary>
    /// Atomically increments the counter for <paramref name="key"/> within the window,
    /// setting a TTL of <paramref name="windowSeconds"/> on first increment.
    /// Returns the new count, or null when the backing store is unavailable (fail-open).
    /// </summary>
    ValueTask<long?> IncrementAsync(string key, int windowSeconds, CancellationToken cancellationToken = default);
}
