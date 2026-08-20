using System.Net;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Heum.Server.xIntegration.Tests;

[Collection(nameof(IntegrationCollection))]
public class TenantAdminEndpointTests(IntegrationFixture fixture) : IAsyncLifetime
{
    private Tenant _tenant = null!;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        db.Tenants.RemoveRange(db.Tenants);

        _tenant = new Tenant { Id = Guid.NewGuid(), Name = "My Tenant", Slug = "my-tenant" };
        db.Tenants.Add(_tenant);
        await db.SaveChangesAsync();

        fixture.FakeEvents.Clear();
        fixture.FakeKeycloak.Reset();
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetMyTenant_Returns200_ForTenantAdmin()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenant.Id));

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(_tenant.Id, response.Content!.Id);
        Assert.Equal("My Tenant", response.Content.Name);
    }

    [Fact]
    public async Task GetMyTenant_Returns401_WithoutToken()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.Anonymous);

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns403_ForSystemAdminRole()
    {
        // SystemAdmin has "SystemAdmin" role but not "Admin", so TenantAdmin policy denies it
        var api = fixture.GetClient<ITenantsApi>(ClientScope.SystemAdmin);

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns400_WhenTokenHasNoTenantIdClaim()
    {
        // Roles present (passes TenantAdmin policy) but no X-Test-Tenant-Id header
        // → TryGetTenantId finds no tenant_id claim → endpoint returns 400
        var api = fixture.GetClient<ITenantsApi>(ClientScope.Authenticated("Admin,User"));

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns404_WhenTenantNotInDatabase()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(Guid.NewGuid()));

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenantUsers_Returns200_ForTenantAdmin()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenant.Id));

        var response = await api.GetMyTenantUsersAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
    }

    [Fact]
    public async Task GetMyTenantUsers_Returns400_WhenTokenHasNoTenantIdClaim()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.Authenticated("Admin,User"));

        var response = await api.GetMyTenantUsersAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddMyTenantUser_Returns201_WithValidRequest()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenant.Id));

        var response = await api.AddMyTenantUserAsync(
            new AddTenantUserRequest { Email = "newmember@mytenant.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("newmember@mytenant.com", response.Content!.Email);
    }

    [Fact]
    public async Task AddMyTenantUser_Returns409_WhenKeycloakThrowsConflict()
    {
        fixture.FakeKeycloak.ExceptionToThrow =
            new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenant.Id));

        var response = await api.AddMyTenantUserAsync(
            new AddTenantUserRequest { Email = "existing@mytenant.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddMyTenantUser_Returns400_WhenTokenHasNoTenantIdClaim()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.Authenticated("Admin,User"));

        var response = await api.AddMyTenantUserAsync(
            new AddTenantUserRequest { Email = "user@example.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EnableMyTenantUser_Returns204_WhenSucceeds()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenant.Id));

        var response = await api.EnableMyTenantUserAsync("some-user-id", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task EnableMyTenantUser_Returns404_WhenUserNotFound()
    {
        fixture.FakeKeycloak.SetTenantUserEnabledResult = false;

        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenant.Id));

        var response = await api.EnableMyTenantUserAsync("missing-user", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DisableMyTenantUser_Returns204_ForDifferentUser()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenant.Id));

        var response = await api.DisableMyTenantUserAsync("other-user-id", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DisableMyTenantUser_Returns400_WhenDisablingOwnAccount()
    {
        // ClientScope.TenantAdmin uses subject "tenant-admin-1" — disabling that same userId
        // triggers the self-disable guard in SetMyTenantUserEnabledAsync / GetKeycloakUserId
        var api = fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenant.Id));

        var response = await api.DisableMyTenantUserAsync("tenant-admin-1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DisableMyTenantUser_Returns400_WhenTokenHasNoTenantIdClaim()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.Authenticated("Admin,User"));

        var response = await api.DisableMyTenantUserAsync("some-user", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
