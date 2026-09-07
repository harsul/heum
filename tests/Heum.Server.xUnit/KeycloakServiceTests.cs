using Heum.Infrastructure.Keycloak.Models;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Server.xUnit.Fakes;

namespace Heum.Server.xUnit;

public sealed class KeycloakServiceTests
{
    private readonly FakeKeycloakAdminClient _adminClient = new();
    private readonly KeycloakService _service;

    public KeycloakServiceTests()
    {
        _service = new KeycloakService(_adminClient);
    }

    [Fact]
    public async Task CreateTenantUserAsync_Throws_WhenRoleIsSystemAdmin()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateTenantUserAsync("u@corp.com", Guid.NewGuid(), "SystemAdmin", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateTenantUserAsync_CreatesUserWithRole_WhenRoleProvided()
    {
        var tenantId = Guid.NewGuid();
        _adminClient.UserIdToReturn = "kc-123";

        var result = await _service.CreateTenantUserAsync("user@corp.com", tenantId, "Admin", TestContext.Current.CancellationToken);

        Assert.Equal("kc-123", result);
        Assert.Single(_adminClient.CreatedUserEmails, "user@corp.com");
    }

    [Fact]
    public async Task CreateTenantUserAsync_CreatesUserWithoutRole_WhenRoleNull()
    {
        var tenantId = Guid.NewGuid();

        await _service.CreateTenantUserAsync("user@corp.com", tenantId, null, TestContext.Current.CancellationToken);

        Assert.Single(_adminClient.CreatedUserEmails);
    }

    [Fact]
    public async Task GetAssignableRolesAsync_FiltersToApplicationRoles()
    {
        _adminClient.RolesToReturn =
        [
            new KeycloakRoleRepresentation { Name = "Admin", Attributes = new() { ["roleType"] = ["Application"] } },
            new KeycloakRoleRepresentation { Name = "User", Attributes = new() { ["roleType"] = ["Application"] } },
            new KeycloakRoleRepresentation { Name = "SystemAdmin", Attributes = new() { ["roleType"] = ["System"] } },
            new KeycloakRoleRepresentation { Name = "InternalRole" },
        ];

        var roles = await _service.GetAssignableRolesAsync(TestContext.Current.CancellationToken);

        // "User" is excluded by name, non-Application and no-attribute roles are excluded
        Assert.Contains("Admin", roles);
        Assert.DoesNotContain("User", roles);
        Assert.DoesNotContain("SystemAdmin", roles);
        Assert.DoesNotContain("InternalRole", roles);
    }

    [Fact]
    public async Task ListTenantUsersAsync_DelegatesToSearchWithTenantQuery()
    {
        var tenantId = Guid.NewGuid();
        _adminClient.UsersToReturn = [new KeycloakUserSummary { Id = "u1", Email = "u@corp.com" }];

        var result = await _service.ListTenantUsersAsync(tenantId, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("u1", result[0].Id);
    }

    [Fact]
    public async Task SetTenantUserEnabledAsync_ReturnsFalse_WhenUserNotFound()
    {
        _adminClient.UserToReturn = null;

        var result = await _service.SetTenantUserEnabledAsync(Guid.NewGuid(), "user-id", true, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task SetTenantUserEnabledAsync_ReturnsFalse_WhenUserNotInTenant()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _adminClient.UserToReturn = new KeycloakUserSummary
        {
            Id = "user-id",
            Attributes = new() { ["tenant_id"] = [otherTenantId.ToString()] },
        };

        var result = await _service.SetTenantUserEnabledAsync(tenantId, "user-id", true, TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.Empty(_adminClient.EnabledCalls);
    }

    [Fact]
    public async Task SetTenantUserEnabledAsync_EnablesUser_WhenUserBelongsToTenant()
    {
        var tenantId = Guid.NewGuid();
        _adminClient.UserToReturn = new KeycloakUserSummary
        {
            Id = "user-id",
            Attributes = new() { ["tenant_id"] = [tenantId.ToString()] },
        };

        var result = await _service.SetTenantUserEnabledAsync(tenantId, "user-id", true, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Single(_adminClient.EnabledCalls);
        Assert.True(_adminClient.EnabledCalls[0].Enabled);
    }

    [Fact]
    public async Task SendRequiredActionsEmailAsync_DelegatesToAdminClient()
    {
        await _service.SendRequiredActionsEmailAsync("user-id", ["UPDATE_PASSWORD"], TestContext.Current.CancellationToken);

        Assert.Single(_adminClient.EmailActionCalls);
        Assert.Equal("user-id", _adminClient.EmailActionCalls[0].UserId);
    }
}
