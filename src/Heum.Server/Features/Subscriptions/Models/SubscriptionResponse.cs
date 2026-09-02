namespace Heum.Server.Features.Subscriptions.Models;

public sealed class SubscriptionResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? ChangedByUserId { get; init; }
    public DateTime EffectiveAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
