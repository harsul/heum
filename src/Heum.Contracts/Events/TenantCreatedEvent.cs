namespace Heum.Contracts.Events;

public record TenantCreatedEvent(
    Guid TenantId,
    string Slug,
    string AdminEmail,
    string AdminFirstName,
    string AdminLastName,
    string KeycloakUserId,
    DateTimeOffset OccurredAt);
