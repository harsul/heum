using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Data.Models;
using Heum.Server.Features.Plans.Services;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Subscriptions.Services;

internal sealed class SubscriptionService(
    HeumDbContext db,
    IEntitlementService entitlementService,
    IDomainEventCollector domainEventCollector,
    TimeProvider timeProvider) : ISubscriptionService
{
    public async Task<AssignPlanResult> AssignPlanAsync(
        Guid tenantId, Guid planId, string? notes, string? changedByUserId, CancellationToken ct = default)
    {
        // Tenant is not ITenantEntity, so this lookup is not affected by the global filter and is
        // valid from an admin (no tenant claim) context.
        if (!await db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            return AssignPlanResult.Failed(AssignPlanFailure.TenantNotFound);

        var plan = await db.Plans.FindAsync([planId], ct);
        if (plan is null)
            return AssignPlanResult.Failed(AssignPlanFailure.PlanNotFound);
        if (!plan.IsActive)
            return AssignPlanResult.Failed(AssignPlanFailure.PlanInactive);

        var previous = await GetCurrentSubscriptionAsync(tenantId, ct);
        var previousPlanId = previous?.PlanId;

        var reason = (previous, planId) switch
        {
            (null, _) => SubscriptionChangeReason.Initial,
            _ => SubscriptionChangeReason.AdminOverride,
        };

        var subscription = TenantSubscription.Record(tenantId, planId, reason, notes, changedByUserId, timeProvider);
        db.TenantSubscriptions.Add(subscription);

        domainEventCollector.Enqueue(new TenantPlanChangedEvent(tenantId, planId, previousPlanId, timeProvider.GetUtcNow()));

        await db.SaveChangesAsync(ct);

        await entitlementService.UpdatePlanMembershipAsync(tenantId, planId, previousPlanId, ct);
        await entitlementService.InvalidateTenantAsync(tenantId, ct);

        await db.Entry(subscription).Reference(s => s.Plan).LoadAsync(ct);
        return AssignPlanResult.Success(subscription);
    }

    public async Task<TenantSubscription?> GetCurrentSubscriptionAsync(Guid tenantId, CancellationToken ct = default) =>
        await db.TenantSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.EffectiveAtUtc)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TenantSubscription>> GetSubscriptionHistoryAsync(Guid tenantId, CancellationToken ct = default) =>
        await db.TenantSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.EffectiveAtUtc)
            .ToListAsync(ct);

    public async Task<TenantEntitlementOverride?> UpsertOverrideAsync(
        Guid tenantId, string entitlementKey, string value, string? reason, CancellationToken ct = default)
    {
        if (!await db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            return null;

        var entitlement = await db.Entitlements.FirstOrDefaultAsync(e => e.Key == entitlementKey && e.IsActive, ct);
        if (entitlement is null)
            return null;

        var existing = await db.TenantEntitlementOverrides
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.EntitlementId == entitlement.Id, ct);

        if (existing is not null)
        {
            existing.SetValue(value, reason);
        }
        else
        {
            existing = TenantEntitlementOverride.Create(tenantId, entitlement.Id, value, reason, timeProvider);
            db.TenantEntitlementOverrides.Add(existing);
        }

        await db.SaveChangesAsync(ct);
        await entitlementService.InvalidateTenantAsync(tenantId, ct);

        return existing;
    }

    public async Task<bool> RemoveOverrideAsync(Guid tenantId, string entitlementKey, CancellationToken ct = default)
    {
        var entitlement = await db.Entitlements.FirstOrDefaultAsync(e => e.Key == entitlementKey, ct);
        if (entitlement is null) return false;

        var existing = await db.TenantEntitlementOverrides
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.EntitlementId == entitlement.Id, ct);

        if (existing is null) return false;

        db.TenantEntitlementOverrides.Remove(existing);
        await db.SaveChangesAsync(ct);
        await entitlementService.InvalidateTenantAsync(tenantId, ct);

        return true;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetResolvedEntitlementsAsync(Guid tenantId, CancellationToken ct = default) =>
        await entitlementService.GetAllAsync(tenantId, ct);
}
