namespace Heum.Data.Models;

public sealed class PlanEntitlement
{
    public Guid PlanId { get; private set; }
    public Guid EntitlementId { get; private set; }
    public string Value { get; private set; } = string.Empty;

    public Plan Plan { get; private set; } = null!;
    public Entitlement Entitlement { get; private set; } = null!;

    private PlanEntitlement() { }

    internal static PlanEntitlement Create(Guid planId, Guid entitlementId, string value) => new()
    {
        PlanId = planId,
        EntitlementId = entitlementId,
        Value = value,
    };

    internal void SetValue(string value) => Value = value;
}
