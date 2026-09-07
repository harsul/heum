using Heum.Server.Features.Plans.Services;

namespace Heum.Server.xUnit.Fakes;

public sealed class FakeEntitlementService : IEntitlementService
{
    public int IntToReturn { get; set; } = int.MaxValue;
    public bool BoolToReturn { get; set; } = true;
    public string? StringToReturn { get; set; }
    public IReadOnlyDictionary<string, string> AllToReturn { get; set; } = new Dictionary<string, string>();

    public ValueTask<IReadOnlyDictionary<string, string>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
        => new(AllToReturn);

    public ValueTask<string?> GetAsync(Guid tenantId, string key, CancellationToken ct = default)
        => new(StringToReturn);

    public ValueTask<int> GetIntAsync(Guid tenantId, string key, int fallback = 0, CancellationToken ct = default)
        => new(IntToReturn == int.MaxValue ? fallback : IntToReturn);

    public ValueTask<bool> GetBoolAsync(Guid tenantId, string key, bool fallback = false, CancellationToken ct = default)
        => new(BoolToReturn);

    public Task InvalidateTenantAsync(Guid tenantId, CancellationToken ct = default) => Task.CompletedTask;
    public Task InvalidatePlanAsync(Guid planId, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdatePlanMembershipAsync(Guid tenantId, Guid newPlanId, Guid? previousPlanId, CancellationToken ct = default) => Task.CompletedTask;
}
