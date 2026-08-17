namespace Heum.Contracts.Events;

/// <summary>
/// Raised whenever a new Keycloak user (a tenant's first admin, or an additional tenant user)
/// needs to complete onboarding: set their name/surname and password, and verify their email,
/// all via a single emailed action link.
/// </summary>
public record UserOnboardingRequestedEvent(
    Guid TenantId,
    string Email,
    string KeycloakUserId,
    DateTimeOffset OccurredAt);
