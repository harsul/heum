using Heum.Infrastructure.Keycloak.Models;
using Heum.Infrastructure.Keycloak.Services;

namespace Heum.Infrastructure.Keycloak.Clients;

/// <summary>
/// Thin wrapper around the Keycloak Admin REST API endpoints. Deliberately has no knowledge
/// of tenants or any other business concepts - it only knows how to call Keycloak. Business
/// operations live in <see cref="IKeycloakService"/>, which is the only consumer of this
/// interface (kept internal on purpose so other projects can't bypass that business logic).
/// </summary>
internal interface IKeycloakAdminClient
{
    /// <summary>Calls <c>POST /admin/realms/{realm}/users</c>.</summary>
    /// <returns>The Keycloak user id (subject) of the newly created user.</returns>
    Task<string> CreateUserAsync(
        KeycloakUserRepresentation user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls <c>GET /admin/realms/{realm}/users?q={query}</c>, where <paramref name="query"/>
    /// is a pre-built Keycloak search query (e.g. an attribute filter like "tenant_id:{id}").
    /// </summary>
    Task<IReadOnlyList<KeycloakUserSummary>> SearchUsersAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>Calls <c>PUT /admin/realms/{realm}/users/{userId}/execute-actions-email</c>.</summary>
    Task ExecuteUserActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);
}
