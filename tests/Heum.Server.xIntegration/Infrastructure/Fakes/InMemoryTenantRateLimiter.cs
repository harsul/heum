using System.Collections.Concurrent;
using Heum.Server.Middleware;

namespace Heum.Server.xIntegration.Infrastructure.Fakes;

/// <summary>
/// Thread-safe in-memory rate limiter used in integration tests instead of the Redis-backed one.
/// Supports configurable per-key limits so tests can drive throttling without a real Redis instance.
/// </summary>
public sealed class InMemoryTenantRateLimiter : ITenantRateLimiter
{
    private readonly ConcurrentDictionary<string, long> _counters = new();

    public ValueTask<long?> IncrementAsync(string key, int windowSeconds, CancellationToken cancellationToken = default)
    {
        var count = _counters.AddOrUpdate(key, 1, (_, existing) => existing + 1);
        return new ValueTask<long?>((long?)count);
    }

    public void Reset() => _counters.Clear();
}
