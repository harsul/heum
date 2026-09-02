namespace Heum.Contracts.Events;

public record PlanEntitlementChangedEvent(
    Guid PlanId,
    string EntitlementKey,
    string? OldValue,
    string NewValue,
    DateTimeOffset OccurredAt) : IDomainEvent;
