using Heum.Data.Models;

namespace Heum.Server.Features.Plans.Services;

public interface IPlanAdminService
{
    Task<IReadOnlyList<Plan>> ListPlansAsync(CancellationToken ct = default);
    Task<Plan?> GetPlanAsync(Guid id, CancellationToken ct = default);
    Task<Plan> CreatePlanAsync(string name, CancellationToken ct = default);
    Task<Plan?> UpdatePlanAsync(Guid id, string name, bool isActive, CancellationToken ct = default);
    Task<bool> UpsertPlanEntitlementAsync(Guid planId, string entitlementKey, string value, CancellationToken ct = default);

    Task<IReadOnlyList<Entitlement>> ListEntitlementsAsync(CancellationToken ct = default);
    Task<Entitlement> CreateEntitlementAsync(string key, EntitlementType type, string? description, CancellationToken ct = default);
}
