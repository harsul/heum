using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Plans.Services;

internal sealed class PlanAdminService(
    HeumDbContext db,
    IEntitlementService entitlementService,
    IDomainEventCollector domainEventCollector,
    TimeProvider timeProvider) : IPlanAdminService
{
    public async Task<IReadOnlyList<Plan>> ListPlansAsync(CancellationToken ct = default) =>
        await db.Plans.Include(p => p.Entitlements).ThenInclude(pe => pe.Entitlement).ToListAsync(ct);

    public async Task<Plan?> GetPlanAsync(Guid id, CancellationToken ct = default) =>
        await db.Plans.Include(p => p.Entitlements).ThenInclude(pe => pe.Entitlement)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Plan?> CreatePlanAsync(string name, CancellationToken ct = default)
    {
        // Backed by a unique index on Plans.Name; this pre-check turns the common case into a
        // clean 409 instead of a DbUpdateException surfacing as 500.
        if (await db.Plans.AnyAsync(p => p.Name == name, ct))
            return null;

        var plan = Plan.Create(name, timeProvider);
        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<Plan?> UpdatePlanAsync(Guid id, string name, bool isActive, CancellationToken ct = default)
    {
        var plan = await db.Plans.FindAsync([id], ct);
        if (plan is null) return null;

        plan.Rename(name, timeProvider);
        plan.SetActive(isActive, timeProvider);

        await db.SaveChangesAsync(ct);

        // If the plan was deactivated, invalidate all tenants on it.
        if (!isActive)
            await entitlementService.InvalidatePlanAsync(id, ct);

        return plan;
    }

    public async Task<bool> UpsertPlanEntitlementAsync(Guid planId, string entitlementKey, string value, CancellationToken ct = default)
    {
        var plan = await db.Plans
            .Include(p => p.Entitlements)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;

        var entitlement = await db.Entitlements.FirstOrDefaultAsync(e => e.Key == entitlementKey && e.IsActive, ct);
        if (entitlement is null) return false;

        var existing = plan.Entitlements.FirstOrDefault(pe => pe.EntitlementId == entitlement.Id);
        var oldValue = existing?.Value;

        // UpsertEntitlement on the aggregate handles both add and update.
        plan.UpsertEntitlement(entitlement.Id, value, timeProvider);

        domainEventCollector.Enqueue(new PlanEntitlementChangedEvent(planId, entitlementKey, oldValue, value, timeProvider.GetUtcNow()));

        await db.SaveChangesAsync(ct);
        await entitlementService.InvalidatePlanAsync(planId, ct);

        return true;
    }

    public async Task<IReadOnlyList<Entitlement>> ListEntitlementsAsync(CancellationToken ct = default) =>
        await db.Entitlements.OrderBy(e => e.Key).ToListAsync(ct);

    public async Task<Entitlement?> CreateEntitlementAsync(string key, EntitlementType type, string? description, CancellationToken ct = default)
    {
        if (await db.Entitlements.AnyAsync(e => e.Key == key, ct))
            return null;

        var entitlement = Entitlement.Create(key, type, description);
        db.Entitlements.Add(entitlement);
        await db.SaveChangesAsync(ct);
        return entitlement;
    }
}
