using Heum.Data.Models;

namespace Heum.Server.Features.Subscriptions.Services;

public interface ISubscriptionService
{
    /// <summary>Assigns a plan to a tenant, writing a new ledger row. Auto-detects the change reason.</summary>
    Task<TenantSubscription> AssignPlanAsync(Guid tenantId, Guid planId, string? notes, string? changedByUserId, CancellationToken ct = default);

    Task<TenantSubscription?> GetCurrentSubscriptionAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantSubscription>> GetSubscriptionHistoryAsync(Guid tenantId, CancellationToken ct = default);

    // Tenant-specific entitlement overrides
    Task<TenantEntitlementOverride> UpsertOverrideAsync(Guid tenantId, string entitlementKey, string value, string? reason, CancellationToken ct = default);
    Task<bool> RemoveOverrideAsync(Guid tenantId, string entitlementKey, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetResolvedEntitlementsAsync(Guid tenantId, CancellationToken ct = default);
}
