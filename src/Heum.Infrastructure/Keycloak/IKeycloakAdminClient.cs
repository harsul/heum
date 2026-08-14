namespace Heum.Infrastructure.Keycloak;

public interface IKeycloakAdminClient
{
    /// <summary>
    /// Creates a new user in Keycloak and stamps the provided tenant id onto the user as a
    /// custom attribute so JWTs issued for this user carry their tenant context.
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
    /// Asks Keycloak to email the user a link that executes the given required actions
    /// (for example "VERIFY_EMAIL"). The email is delivered through the realm's SMTP settings.
    /// </summary>
    Task SendRequiredActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);
}
