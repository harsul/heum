using Heum.Data.Models;
using Heum.Server.Features.Subscriptions.Services;

namespace Heum.Server.xUnit.Fakes;

/// <summary>No-op subscription service for unit tests that don't exercise plan assignment.</summary>
public sealed class FakeSubscriptionService : ISubscriptionService
{
    public Task<AssignPlanResult> AssignPlanAsync(Guid tenantId, Guid planId, string? notes, string? changedByUserId, CancellationToken ct = default)
        => Task.FromResult(AssignPlanResult.Success(
            TenantSubscription.Record(tenantId, planId, SubscriptionChangeReason.Initial, notes, changedByUserId, TimeProvider.System)));

    public Task<TenantSubscription?> GetCurrentSubscriptionAsync(Guid tenantId, CancellationToken ct = default)
        => Task.FromResult<TenantSubscription?>(null);

    public Task<IReadOnlyList<TenantSubscription>> GetSubscriptionHistoryAsync(Guid tenantId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TenantSubscription>>([]);

    public Task<TenantEntitlementOverride?> UpsertOverrideAsync(Guid tenantId, string entitlementKey, string value, string? reason, CancellationToken ct = default)
        => Task.FromResult<TenantEntitlementOverride?>(null);

    public Task<bool> RemoveOverrideAsync(Guid tenantId, string entitlementKey, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<IReadOnlyDictionary<string, string>> GetResolvedEntitlementsAsync(Guid tenantId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
}
