using Heum.Infrastructure.Keycloak.Models;
using Heum.Infrastructure.Keycloak.Services;

namespace Heum.Server.xUnit.Fakes;

/// <summary>Simple hand-written test double for <see cref="IKeycloakService"/> (no mocking library in this project).</summary>
public sealed class FakeKeycloakService : IKeycloakService
{
    public Exception? ExceptionToThrow { get; set; }
    public string UserIdToReturn { get; set; } = Guid.NewGuid().ToString();
    public int CreateTenantUserCallCount { get; private set; }

    public Task<string> CreateTenantUserAsync(
        string email,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        CreateTenantUserCallCount++;

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
}
