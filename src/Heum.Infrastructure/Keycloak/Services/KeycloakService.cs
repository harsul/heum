using Heum.Infrastructure.Keycloak.Clients;
using Heum.Infrastructure.Keycloak.Models;

namespace Heum.Infrastructure.Keycloak.Services;

/// <inheritdoc cref="IKeycloakService" />
internal sealed class KeycloakService(IKeycloakAdminClient adminClient) : IKeycloakService
{
    public Task<string> ProvisionTenantAdminUserAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => CreateKeycloakUserAsync(username, email, firstName, lastName, password, tenantId, cancellationToken);

    public Task<string> CreateTenantUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => CreateKeycloakUserAsync(username: email, email, firstName, lastName, password, tenantId, cancellationToken);

    public async Task<IReadOnlyList<KeycloakUserSummary>> ListTenantUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Keycloak's user search supports querying custom attributes via "q=key:value".
        var query = $"tenant_id:{tenantId}";
        return await adminClient.SearchUsersAsync(query, cancellationToken);
    }

    public Task SendRequiredActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default)
        => adminClient.ExecuteUserActionsEmailAsync(userId, actions, cancellationToken);

    private async Task<string> CreateKeycloakUserAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var user = new KeycloakUserRepresentation
        {
            Username = username,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Attributes = new Dictionary<string, string[]>
            {
                ["tenant_id"] = [tenantId.ToString()],
            },
            Credentials =
            [
                new KeycloakCredentialRepresentation { Type = "password", Value = password, Temporary = false },
            ],
        };

        return await adminClient.CreateUserAsync(user, cancellationToken);
    }
}
