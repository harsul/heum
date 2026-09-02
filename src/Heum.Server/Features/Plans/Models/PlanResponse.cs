namespace Heum.Server.Features.Plans.Models;

public sealed class PlanResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public IReadOnlyList<PlanEntitlementResponse> Entitlements { get; init; } = [];
}

public sealed class PlanEntitlementResponse
{
    public string Key { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
