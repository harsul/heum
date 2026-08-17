using Heum.Infrastructure.Keycloak.Models;

namespace Heum.Infrastructure.Keycloak;

/// <summary>
/// Tenant-oriented Keycloak operations used by the rest of the app. This is the intended
/// entry point for interacting with Keycloak - it's built on top of <see cref="IKeycloakAdminClient"/>
/// (internal, raw endpoint calls only) and owns all the "what does this mean for our domain"
/// logic (e.g. stamping the tenant id attribute, building search queries).
/// </summary>
public interface IKeycloakService
{
    /// <summary>
    /// Creates a tenant's first (admin) user in Keycloak and stamps the provided tenant id
    /// onto the user as a custom attribute so JWTs issued for this user carry their tenant
    /// context.
    /// </summary>
    /// <returns>The Keycloak user id (subject) of the newly created user.</returns>
    Task<string> ProvisionTenantAdminUserAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an additional user for an existing tenant (as opposed to the tenant's first
    /// admin user provisioned via <see cref="ProvisionTenantAdminUserAsync"/>), stamped with
    /// the same tenant id attribute.
    /// </summary>
    /// <returns>The Keycloak user id (subject) of the newly created user.</returns>
    Task<string> CreateTenantUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
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
    /// (for example "VERIFY_EMAIL"). The email is delivered through the realm's SMTP settings.
    /// </summary>
    Task SendRequiredActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);
}
