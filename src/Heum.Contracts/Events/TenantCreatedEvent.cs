namespace Heum.Contracts.Events;

public record TenantCreatedEvent(
    Guid TenantId,
    string Slug,
    string AdminEmail,
    string KeycloakUserId,
    DateTimeOffset OccurredAt);
