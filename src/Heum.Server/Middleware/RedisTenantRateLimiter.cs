using StackExchange.Redis;

namespace Heum.Server.Middleware;

internal sealed class RedisTenantRateLimiter(IConnectionMultiplexer redis, ILogger<RedisTenantRateLimiter> logger) : ITenantRateLimiter
{
    // Atomically increments the key and sets TTL only on first increment.
    private const string IncrScript = """
        local c = redis.call('INCR', KEYS[1])
        if c == 1 then redis.call('EXPIRE', KEYS[1], ARGV[1]) end
        return c
        """;

    public async ValueTask<long?> IncrementAsync(string key, int windowSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var result = await db.ScriptEvaluateAsync(IncrScript, [(RedisKey)key], [(RedisValue)windowSeconds]);
            return (long)result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for tenant rate limiting — failing open");
            return null;
        }
    }
}
