using System.Text.Json;
using Heum.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Heum.Server.Features.Plans.Services;

// Redis (IConnectionMultiplexer) is required for caching. All Redis calls are wrapped in
// try/catch: on failure the service falls back to the database and logs a warning (fail-open).
internal sealed class EntitlementService(
    HeumDbContext db,
    IConnectionMultiplexer redis,
    ILogger<EntitlementService> logger) : IEntitlementService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static string TenantKey(Guid tenantId) => $"entitlements:tenant:{tenantId}";
    private static string PlanMembersKey(Guid planId) => $"plan:tenants:{planId}";

    public async ValueTask<IReadOnlyDictionary<string, string>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var redisDb = redis.GetDatabase();
            var cached = await redisDb.StringGetAsync(TenantKey(tenantId));
            if (cached.HasValue)
                return JsonSerializer.Deserialize<Dictionary<string, string>>(cached.ToString())
                    ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable when reading entitlements for tenant {TenantId} — falling back to DB", tenantId);
        }

        return await LoadFromDbAndCacheAsync(tenantId, ct);
    }

    public async ValueTask<string?> GetAsync(Guid tenantId, string key, CancellationToken ct = default)
    {
        var all = await GetAllAsync(tenantId, ct);
        return all.TryGetValue(key, out var value) ? value : null;
    }

    public async ValueTask<int> GetIntAsync(Guid tenantId, string key, int fallback = 0, CancellationToken ct = default)
    {
        var value = await GetAsync(tenantId, key, ct);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public async ValueTask<bool> GetBoolAsync(Guid tenantId, string key, bool fallback = false, CancellationToken ct = default)
    {
        var value = await GetAsync(tenantId, key, ct);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public async Task InvalidateTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(TenantKey(tenantId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable when invalidating entitlements for tenant {TenantId}", tenantId);
        }
    }

    public async Task InvalidatePlanAsync(Guid planId, CancellationToken ct = default)
    {
        try
        {
            var redisDb = redis.GetDatabase();
            var members = await redisDb.SetMembersAsync(PlanMembersKey(planId));

            IEnumerable<Guid> tenantIds;
            if (members.Length > 0)
            {
                tenantIds = members.Select(m => Guid.Parse(m.ToString()));
            }
            else
            {
                // Set is gone (Redis restart or never populated) — fall back to DB.
                tenantIds = await db.TenantSubscriptions
                    .IgnoreQueryFilters()
                    .Where(s => s.PlanId == planId)
                    .Select(s => s.TenantId)
                    .Distinct()
                    .ToListAsync(ct);
            }

            var keys = tenantIds.Select(id => (RedisKey)TenantKey(id)).ToArray();
            if (keys.Length > 0)
                await redisDb.KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable when invalidating entitlements for plan {PlanId}", planId);
        }
    }

    public async Task UpdatePlanMembershipAsync(Guid tenantId, Guid newPlanId, Guid? previousPlanId, CancellationToken ct = default)
    {
        try
        {
            var redisDb = redis.GetDatabase();
            if (previousPlanId.HasValue)
                await redisDb.SetRemoveAsync(PlanMembersKey(previousPlanId.Value), tenantId.ToString());
            await redisDb.SetAddAsync(PlanMembersKey(newPlanId), tenantId.ToString());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable when updating plan membership for tenant {TenantId}", tenantId);
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadFromDbAndCacheAsync(Guid tenantId, CancellationToken ct)
    {
        // Current subscription → plan → entitlements
        var subscription = await db.TenantSubscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.EffectiveAtUtc)
            .FirstOrDefaultAsync(ct);

        Dictionary<string, string> result;

        if (subscription is null)
        {
            result = new Dictionary<string, string>();
        }
        else
        {
            var planEntitlements = await db.PlanEntitlements
                .Where(pe => pe.PlanId == subscription.PlanId)
                .Include(pe => pe.Entitlement)
                .ToListAsync(ct);

            result = planEntitlements.ToDictionary(
                pe => pe.Entitlement.Key,
                pe => pe.Value);

            // Tenant overrides win over plan defaults
            var overrides = await db.TenantEntitlementOverrides
                .IgnoreQueryFilters()
                .Where(o => o.TenantId == tenantId)
                .Include(o => o.Entitlement)
                .ToListAsync(ct);

            foreach (var o in overrides)
                result[o.Entitlement.Key] = o.Value;
        }

        try
        {
            var json = JsonSerializer.Serialize(result);
            await redis.GetDatabase().StringSetAsync(TenantKey(tenantId), json, CacheTtl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable when caching entitlements for tenant {TenantId}", tenantId);
        }

        return result;
    }
}
