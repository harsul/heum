namespace Heum.Data.Models;

public enum SubscriptionChangeReason { Initial, Upgrade, Downgrade, AdminOverride }

/// <summary>
/// Append-only ledger of plan assignments. The active plan is the row with the highest
/// <see cref="EffectiveAtUtc"/> for a given tenant.
/// </summary>
public sealed class TenantSubscription
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionChangeReason Reason { get; private set; }
    public string? Notes { get; private set; }
    public string? ChangedByUserId { get; private set; }
    public DateTime EffectiveAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Plan Plan { get; private set; } = null!;

    private TenantSubscription() { }

    public static TenantSubscription Record(
        Guid tenantId,
        Guid planId,
        SubscriptionChangeReason reason,
        string? notes,
        string? changedByUserId,
        TimeProvider timeProvider) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        PlanId = planId,
        Reason = reason,
        Notes = notes,
        ChangedByUserId = changedByUserId,
        EffectiveAtUtc = timeProvider.GetUtcNow().UtcDateTime,
        CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
    };
}
