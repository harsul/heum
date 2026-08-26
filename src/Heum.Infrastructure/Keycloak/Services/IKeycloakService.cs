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
    /// attribute, and flagged with the required action needed to complete onboarding
    /// (set a password) the next time they authenticate.
    /// </summary>
    /// <param name="role">
    /// An additional realm role to grant the user on top of the baseline "User" role
    /// (e.g. "Admin"). Pass <c>null</c> to create a plain user with "User" only.
    /// Must never be "SystemAdmin" — that role is reserved for platform operators and
    /// is rejected by <see cref="KeycloakService"/> as a belt-and-suspenders guard.
    /// </param>
    /// <returns>The Keycloak user id (subject) of the newly created user.</returns>
    Task<string> CreateTenantUserAsync(
        string email,
        Guid tenantId,
        string? role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the realm roles tagged with <c>roleType=Application</c> in Keycloak,
    /// excluding the base "User" role (which is always assigned automatically).
    /// These are the roles callers may request when creating a tenant user.
    /// </summary>
    Task<IReadOnlyList<string>> GetAssignableRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up all Keycloak users stamped with the given tenant id (via the "tenant_id"
    /// custom attribute set during provisioning).
    /// </summary>
    Task<IReadOnlyList<KeycloakUserSummary>> ListTenantUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks Keycloak to email the user a link that executes the given required actions
    /// (for example "UPDATE_PASSWORD"). The email is delivered through the realm's SMTP settings.
    /// </summary>
    Task SendRequiredActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a Keycloak user, after verifying they're stamped with the given
    /// tenant's id (so a tenant admin can't disable a user outside their own tenant).
    /// </summary>
    /// <returns><c>false</c> if the user doesn't exist or doesn't belong to this tenant.</returns>
    Task<bool> SetTenantUserEnabledAsync(
        Guid tenantId,
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default);
}
