using Heum.Contracts.Events;
using Heum.Functions.Handlers;
using Heum.Infrastructure.Keycloak.Models;
using Heum.Infrastructure.Keycloak.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Heum.Server.xUnit;

public sealed class UserOnboardingHandlerTests
{
    private readonly FakeIdentityProvider _idp = new();

    [Fact]
    public async Task HandleAsync_SendsUpdatePasswordEmail_ToCorrectUser()
    {
        var handler = new UserOnboardingHandler(_idp, NullLogger<UserOnboardingHandler>.Instance);
        var evt = new UserOnboardingRequestedEvent(
            TenantId: Guid.NewGuid(),
            Email: "user@example.com",
            KeycloakUserId: "user-abc",
            OccurredAt: DateTimeOffset.UtcNow);

        await handler.HandleAsync(evt, CancellationToken.None);

        Assert.Single(_idp.Calls);
        var (userId, actions) = _idp.Calls[0];
        Assert.Equal("user-abc", userId);
        Assert.Equal(["UPDATE_PASSWORD"], actions);
    }

    private sealed class FakeIdentityProvider : IIdentityProviderService
    {
        public List<(string UserId, IEnumerable<string> Actions)> Calls { get; } = [];

        public Task<string> CreateTenantUserAsync(string email, Guid tenantId, string? role, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.NewGuid().ToString());

        public Task<IReadOnlyList<string>> GetAssignableRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Admin"]);

        public Task<IReadOnlyList<KeycloakUserSummary>> ListTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<KeycloakUserSummary>>([]);

        public Task SendRequiredActionsEmailAsync(string userId, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        {
            Calls.Add((userId, actions.ToList()));
            return Task.CompletedTask;
        }

        public Task<bool> SetTenantUserEnabledAsync(Guid tenantId, string userId, bool enabled, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
