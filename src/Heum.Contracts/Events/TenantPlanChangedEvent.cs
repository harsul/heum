namespace Heum.Contracts.Events;

public record TenantPlanChangedEvent(
    Guid TenantId,
    Guid NewPlanId,
    Guid? PreviousPlanId,
    DateTimeOffset OccurredAt) : IDomainEvent;
