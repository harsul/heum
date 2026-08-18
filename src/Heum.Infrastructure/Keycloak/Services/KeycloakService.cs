using Heum.Infrastructure.Keycloak.Clients;
using Heum.Infrastructure.Keycloak.Models;

namespace Heum.Infrastructure.Keycloak.Services;

/// <inheritdoc cref="IKeycloakService" />
internal sealed class KeycloakService(IKeycloakAdminClient adminClient) : IKeycloakService
{
    private static readonly string[] OnboardingRequiredActions = ["UPDATE_PASSWORD"];

    public async Task<string> CreateTenantUserAsync(
        string email,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var user = new KeycloakUserRepresentation
        {
            Username = email,
            Email = email,
            FirstName = string.Empty,
            LastName = string.Empty,
            EmailVerified = false,
            Attributes = new Dictionary<string, string[]>
            {
                ["tenant_id"] = [tenantId.ToString()],
            },
            Credentials = [],
            RequiredActions = [.. OnboardingRequiredActions],
        };

        return await adminClient.CreateUserAsync(user, cancellationToken);
    }

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
}
