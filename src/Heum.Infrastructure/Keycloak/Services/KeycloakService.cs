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
        string? role,
        CancellationToken cancellationToken = default)
    {
        if (role == "SystemAdmin")
            throw new InvalidOperationException("The SystemAdmin role cannot be assigned through the API.");

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
            RealmRoles = role is not null ? [role, "User"] : ["User"],
        };

        return await adminClient.CreateUserAsync(user, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAssignableRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await adminClient.GetRolesAsync(cancellationToken);
        return roles
            .Where(r =>
                r.Attributes is { } attrs &&
                attrs.TryGetValue("roleType", out var values) &&
                values.Contains("Application") &&
                r.Name != "User")
            .Select(r => r.Name)
            .ToList();
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
