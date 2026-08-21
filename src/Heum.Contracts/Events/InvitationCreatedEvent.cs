namespace Heum.Contracts.Events;

public record InvitationCreatedEvent(
    Guid InvitationId,
    Guid TenantId,
    string Email,
    string Token,
    DateTimeOffset OccurredAt) : IDomainEvent;
