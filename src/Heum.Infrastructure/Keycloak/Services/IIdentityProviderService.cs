using Heum.Infrastructure.Keycloak.Models;

namespace Heum.Infrastructure.Keycloak.Services;

/// <summary>
/// Provider-agnostic identity operations used by the rest of the application.
/// This is the intended entry point for user provisioning and management; it is built on top of
/// <see cref="IKeycloakAdminClient"/> (internal, raw endpoint calls only) and owns all the
/// "what does this mean for our domain" logic.
/// <para>
/// <strong>Keycloak-specific implementation notes</strong> (relevant when swapping to Duende/Entra):
/// <list type="bullet">
/// <item>
///   <description>
///     Users are stamped with a <c>tenant_id</c> custom attribute in Keycloak that corresponds
///     to <c>ITenantEntity.TenantId</c> — used for tenant-scoped user listing and security checks.
///   </description>
/// </item>
/// <item>
///   <description>
///     JWT realm roles are packed into a single <c>realm_access</c> claim by Keycloak and
///     flattened to standard <c>ClaimTypes.Role</c> claims by
///     <c>KeycloakClaimsHelper.AddRealmRoleClaims</c> during token validation.
///   </description>
/// </item>
/// <item>
///   <description>
///     Assignable roles are filtered using a <c>roleType=Application</c> Keycloak role attribute.
///     A replacement provider must expose an equivalent concept (e.g. application roles tagged
///     separately from internal/system roles).
///   </description>
/// </item>
/// <item>
///   <description>
///     <see cref="SendRequiredActionsEmailAsync"/> relies on Keycloak's built-in
///     "execute-actions-email" flow. A Duende or Entra implementation would trigger an
///     equivalent onboarding/password-set email through that provider's own mechanism.
///   </description>
/// </item>
/// </list>
/// </para>
/// </summary>
public interface IIdentityProviderService
{
    /// <summary>
    /// Creates a user for a tenant (whether it's the tenant's first/admin user or an
    /// additional teammate added later - there's no distinction at the identity provider level).
    /// The user is created with no password and no name, stamped with the tenant id
    /// attribute, and flagged with the required action needed to complete onboarding
    /// (set a password) the next time they authenticate.
    /// </summary>
    /// <param name="role">
    /// An additional realm role to grant the user on top of the baseline "User" role
    /// (e.g. "Admin"). Pass <c>null</c> to create a plain user with "User" only.
    /// Must never be "SystemAdmin" — that role is reserved for platform operators.
    /// </param>
    /// <returns>The identity provider user id (subject) of the newly created user.</returns>
    Task<string> CreateTenantUserAsync(
        string email,
        Guid tenantId,
        string? role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the realm roles tagged with <c>roleType=Application</c> in the identity provider,
    /// excluding the base "User" role (which is always assigned automatically).
    /// These are the roles callers may request when creating a tenant user.
    /// </summary>
    Task<IReadOnlyList<string>> GetAssignableRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up all users stamped with the given tenant id (via the "tenant_id"
    /// custom attribute set during provisioning).
    /// </summary>
    Task<IReadOnlyList<KeycloakUserSummary>> ListTenantUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the identity provider to email the user a link that executes the given required actions
    /// (for example "UPDATE_PASSWORD"). The email is delivered through the provider's SMTP settings.
    /// </summary>
    Task SendRequiredActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a user, after verifying they're stamped with the given tenant's id
    /// (so a tenant admin can't disable a user outside their own tenant).
    /// </summary>
    /// <returns><c>false</c> if the user doesn't exist or doesn't belong to this tenant.</returns>
    Task<bool> SetTenantUserEnabledAsync(
        Guid tenantId,
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default);
}
