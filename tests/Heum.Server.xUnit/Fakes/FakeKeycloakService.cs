using Heum.Infrastructure.Keycloak.Models;
using Heum.Infrastructure.Keycloak.Services;

namespace Heum.Server.xUnit.Fakes;

/// <summary>Simple hand-written test double for <see cref="IKeycloakService"/> (no mocking library in this project).</summary>
public sealed class FakeKeycloakService : IKeycloakService
{
    public Exception? ExceptionToThrow { get; set; }
    public string UserIdToReturn { get; set; } = Guid.NewGuid().ToString();
    public int CreateTenantUserCallCount { get; private set; }
    public bool? LastIsTenantAdmin { get; private set; }
    public bool? SetTenantUserEnabledResult { get; set; } = true;

    public void Reset()
    {
        ExceptionToThrow = null;
        UserIdToReturn = Guid.NewGuid().ToString();
        CreateTenantUserCallCount = 0;
        LastIsTenantAdmin = null;
        SetTenantUserEnabledResult = true;
    }

    public Task<string> CreateTenantUserAsync(
        string email,
        Guid tenantId,
        bool isTenantAdmin,
        CancellationToken cancellationToken = default)
    {
        CreateTenantUserCallCount++;
        LastIsTenantAdmin = isTenantAdmin;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        return Task.FromResult(UserIdToReturn);
    }

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
