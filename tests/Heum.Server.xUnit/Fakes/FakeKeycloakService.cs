using Heum.Infrastructure.Keycloak.Models;
using Heum.Infrastructure.Keycloak.Services;

namespace Heum.Server.xUnit.Fakes;

/// <summary>Simple hand-written test double for <see cref="IKeycloakService"/> (no mocking library in this project).</summary>
public sealed class FakeKeycloakService : IKeycloakService
{
    public Exception? ExceptionToThrow { get; set; }
    public string UserIdToReturn { get; set; } = Guid.NewGuid().ToString();
    public int CreateTenantUserCallCount { get; private set; }
    public string? LastRole { get; private set; }
    public bool? SetTenantUserEnabledResult { get; set; } = true;
    public IReadOnlyList<string> AssignableRolesToReturn { get; set; } = ["Admin"];

    public void Reset()
    {
        ExceptionToThrow = null;
        UserIdToReturn = Guid.NewGuid().ToString();
        CreateTenantUserCallCount = 0;
        LastRole = null;
        SetTenantUserEnabledResult = true;
        AssignableRolesToReturn = ["Admin"];
    }

    public Task<string> CreateTenantUserAsync(
        string email,
        Guid tenantId,
        string? role,
        CancellationToken cancellationToken = default)
    {
        CreateTenantUserCallCount++;
        LastRole = role;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        return Task.FromResult(UserIdToReturn);
    }

    public Task<IReadOnlyList<string>> GetAssignableRolesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AssignableRolesToReturn);

    public Task<IReadOnlyList<KeycloakUserSummary>> ListTenantUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeycloakUserSummary>>([]);

    public Task SendRequiredActionsEmailAsync(
        string userId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> SetTenantUserEnabledAsync(
        Guid tenantId,
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default)
        => Task.FromResult(SetTenantUserEnabledResult ?? false);
}
