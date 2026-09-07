using Heum.Data.Models;

namespace Heum.Server.Features.Subscriptions.Services;

public enum AssignPlanFailure
{
    None,
    TenantNotFound,
    PlanNotFound,
    PlanInactive,
}

/// <summary><see cref="Subscription"/> is set on success; otherwise <see cref="Failure"/> says why.</summary>
public sealed record AssignPlanResult(TenantSubscription? Subscription, AssignPlanFailure Failure = AssignPlanFailure.None)
{
    public static AssignPlanResult Success(TenantSubscription subscription) => new(subscription);
    public static AssignPlanResult Failed(AssignPlanFailure failure) => new(null, failure);
}

public interface ISubscriptionService
{
    /// <summary>
    /// Assigns a plan to a tenant, writing a new ledger row. Fails (without writing) when the
    /// tenant does not exist or the plan is missing or inactive.
    /// </summary>
    Task<AssignPlanResult> AssignPlanAsync(Guid tenantId, Guid planId, string? notes, string? changedByUserId, CancellationToken ct = default);

    Task<TenantSubscription?> GetCurrentSubscriptionAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantSubscription>> GetSubscriptionHistoryAsync(Guid tenantId, CancellationToken ct = default);

    // Tenant-specific entitlement overrides

    /// <summary>Returns <c>null</c> when the tenant does not exist or no active entitlement has that key.</summary>
    Task<TenantEntitlementOverride?> UpsertOverrideAsync(Guid tenantId, string entitlementKey, string value, string? reason, CancellationToken ct = default);
    Task<bool> RemoveOverrideAsync(Guid tenantId, string entitlementKey, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetResolvedEntitlementsAsync(Guid tenantId, CancellationToken ct = default);
}
