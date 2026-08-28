namespace Heum.Contracts.Events;

public record TenantCreatedEvent(
    Guid TenantId,
    string Slug,
    DateTimeOffset OccurredAt) : IDomainEvent;
