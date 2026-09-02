using Heum.Server.Features.Plans.Services;

namespace Heum.Server.xIntegration.Infrastructure.Fakes;

/// <summary>
/// Entitlement service that skips all Redis operations in integration tests.
/// Returns empty entitlements so fallback values are used (no limit enforcement during tests).
/// </summary>
public sealed class NoOpEntitlementService : IEntitlementService
{
    public ValueTask<IReadOnlyDictionary<string, string>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
        => new(new Dictionary<string, string>());

    public ValueTask<string?> GetAsync(Guid tenantId, string key, CancellationToken ct = default)
        => new((string?)null);

    public ValueTask<int> GetIntAsync(Guid tenantId, string key, int fallback = 0, CancellationToken ct = default)
        => new(fallback);

    public ValueTask<bool> GetBoolAsync(Guid tenantId, string key, bool fallback = false, CancellationToken ct = default)
        => new(fallback);

    public Task InvalidateTenantAsync(Guid tenantId, CancellationToken ct = default) => Task.CompletedTask;
    public Task InvalidatePlanAsync(Guid planId, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdatePlanMembershipAsync(Guid tenantId, Guid newPlanId, Guid? previousPlanId, CancellationToken ct = default) => Task.CompletedTask;
}
