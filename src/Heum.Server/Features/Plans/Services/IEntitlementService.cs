namespace Heum.Server.Features.Plans.Services;

public interface IEntitlementService
{
    /// <summary>
    /// Returns all resolved entitlements for the tenant (plan defaults merged with overrides),
    /// using Redis as a read-through cache.
    /// </summary>
    ValueTask<IReadOnlyDictionary<string, string>> GetAllAsync(Guid tenantId, CancellationToken ct = default);

    ValueTask<string?> GetAsync(Guid tenantId, string key, CancellationToken ct = default);
    ValueTask<int> GetIntAsync(Guid tenantId, string key, int fallback = 0, CancellationToken ct = default);
    ValueTask<bool> GetBoolAsync(Guid tenantId, string key, bool fallback = false, CancellationToken ct = default);

    /// <summary>Removes the cached entitlements for a single tenant.</summary>
    Task InvalidateTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Removes cached entitlements for every tenant currently on the given plan.</summary>
    Task InvalidatePlanAsync(Guid planId, CancellationToken ct = default);

    /// <summary>
    /// Updates the Redis Set that tracks which tenants are on a plan.
    /// Call this whenever a subscription changes.
    /// </summary>
    Task UpdatePlanMembershipAsync(Guid tenantId, Guid newPlanId, Guid? previousPlanId, CancellationToken ct = default);
}
