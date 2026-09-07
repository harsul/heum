using Heum.Infrastructure.Keycloak.Clients;
using Heum.Infrastructure.Keycloak.Models;

namespace Heum.Server.xUnit.Fakes;

internal sealed class FakeKeycloakAdminClient : IKeycloakAdminClient
{
    public string UserIdToReturn { get; set; } = Guid.NewGuid().ToString();
    public List<KeycloakUserSummary> UsersToReturn { get; set; } = [];
    public List<KeycloakRoleRepresentation> RolesToReturn { get; set; } = [];
    public KeycloakUserSummary? UserToReturn { get; set; }

    public List<string> CreatedUserEmails { get; } = [];
    public List<(string UserId, bool Enabled)> EnabledCalls { get; } = [];
    public List<(string UserId, IEnumerable<string> Actions)> EmailActionCalls { get; } = [];

    public Task<string> CreateUserAsync(KeycloakUserRepresentation user, CancellationToken cancellationToken = default)
    {
        CreatedUserEmails.Add(user.Email ?? string.Empty);
        return Task.FromResult(UserIdToReturn);
    }

    public Task<IReadOnlyList<KeycloakUserSummary>> SearchUsersAsync(string query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeycloakUserSummary>>(UsersToReturn);

    public Task ExecuteUserActionsEmailAsync(string userId, IEnumerable<string> actions, CancellationToken cancellationToken = default)
    {
        EmailActionCalls.Add((userId, actions));
        return Task.CompletedTask;
    }

    public Task<KeycloakUserSummary?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(UserToReturn);

    public Task SetUserEnabledAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
    {
        EnabledCalls.Add((userId, enabled));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<KeycloakRoleRepresentation>> GetRolesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeycloakRoleRepresentation>>(RolesToReturn);
}
