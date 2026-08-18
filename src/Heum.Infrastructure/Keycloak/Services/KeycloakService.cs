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
        bool isTenantAdmin,
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
                [KeycloakClaimTypes.TenantId] = [tenantId.ToString()],
            },
            Credentials = [],
            RequiredActions = [.. OnboardingRequiredActions],
            RealmRoles = isTenantAdmin ? ["Admin", "User"] : ["User"],
        };

        return await adminClient.CreateUserAsync(user, cancellationToken);
    }

    public async Task<IReadOnlyList<KeycloakUserSummary>> ListTenantUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = $"{KeycloakClaimTypes.TenantId}:{tenantId}";
        return await adminClient.SearchUsersAsync(query, cancellationToken);
    }

    public Task SendRequiredActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default)
        => adminClient.ExecuteUserActionsEmailAsync(userId, actions, cancellationToken);

    public async Task<bool> SetTenantUserEnabledAsync(
        Guid tenantId,
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var user = await adminClient.GetUserAsync(userId, cancellationToken);
        if (user is null || !BelongsToTenant(user, tenantId))
            return false;

        await adminClient.SetUserEnabledAsync(userId, enabled, cancellationToken);
        return true;
    }

    private static bool BelongsToTenant(KeycloakUserSummary user, Guid tenantId) =>
        user.Attributes is { } attributes &&
        attributes.TryGetValue(KeycloakClaimTypes.TenantId, out var tenantIds) &&
        tenantIds.Contains(tenantId.ToString());
}
