namespace Heum.Server.Keycloak;

public interface IKeycloakAdminClient
{
    /// <summary>
    /// Creates a new user in Keycloak, assigns them the given realm role, and stamps the
    /// provided tenant id onto the user as a custom attribute so JWTs issued for this user
    /// carry their tenant context.
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
}
