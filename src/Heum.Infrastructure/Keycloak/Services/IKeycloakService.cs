using Heum.Infrastructure.Keycloak.Clients;
using Heum.Infrastructure.Keycloak.Models;

namespace Heum.Infrastructure.Keycloak.Services;

/// <summary>
/// Tenant-oriented Keycloak operations used by the rest of the app. This is the intended
/// entry point for interacting with Keycloak - it's built on top of <see cref="IKeycloakAdminClient"/>
/// (internal, raw endpoint calls only) and owns all the "what does this mean for our domain"
/// logic (e.g. stamping the tenant id attribute, building search queries).
/// </summary>
public interface IKeycloakService
{
    /// <summary>
    /// Creates a user for a tenant (whether it's the tenant's first/admin user or an
    /// additional teammate added later - there's no distinction at the Keycloak level).
    /// The user is created with no password and no name, stamped with the tenant id
    /// attribute, and flagged with the required actions needed to complete onboarding
    /// (update profile, set a password, verify email) the next time they authenticate.
    /// </summary>
    /// <returns>The Keycloak user id (subject) of the newly created user.</returns>
    Task<string> CreateTenantUserAsync(
        string email,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up all Keycloak users stamped with the given tenant id (via the "tenant_id"
    /// custom attribute set during provisioning).
    /// </summary>
    Task<IReadOnlyList<KeycloakUserSummary>> ListTenantUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks Keycloak to email the user a link that executes the given required actions
    /// (for example "UPDATE_PROFILE", "UPDATE_PASSWORD", "VERIFY_EMAIL"). The email is
    /// delivered through the realm's SMTP settings.
    /// </summary>
    Task SendRequiredActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);
}
