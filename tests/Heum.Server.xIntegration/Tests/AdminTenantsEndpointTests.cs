using System.Net;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Admin.Tenants.Models;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heum.Server.xIntegration.Tests;

[Collection(nameof(IntegrationCollection))]
public class AdminTenantsEndpointTests(IntegrationFixture fixture) : IAsyncLifetime
{
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        db.Tenants.RemoveRange(db.Tenants);
        db.Tenants.AddRange(
            new Tenant { Id = Guid.NewGuid(), Name = "Alpha", Slug = "alpha" },
            new Tenant { Id = Guid.NewGuid(), Name = "Beta", Slug = "beta" });
        await db.SaveChangesAsync();

        fixture.FakeEvents.Clear();
        fixture.FakeKeycloak.Reset();
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ListTenants_Returns200WithAll_ForSystemAdmin()
    {
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.ListTenantsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, response.Content!.Count);
    }

    [Fact]
    public async Task ListTenants_Returns401_WithoutToken()
    {
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.Anonymous);

        var response = await api.ListTenantsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListTenants_Returns403_ForTenantAdminRole()
    {
        // TenantAdmin has "Admin" role but /api/admin/tenants requires "SystemAdmin"
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.TenantAdmin(Guid.NewGuid()));

        var response = await api.ListTenantsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTenant_Returns200_ForExistingTenant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.GetTenantAsync(tenant.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(tenant.Id, response.Content!.Id);
    }

    [Fact]
    public async Task GetTenant_Returns404_ForUnknownId()
    {
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.GetTenantAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTenant_Returns201_WithValidRequest()
    {
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.CreateTenantAsync(
            new CreateTenantRequest { CompanyName = "New Corp", AdminEmail = "admin@newcorp.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotEqual(Guid.Empty, response.Content!.Id);
        Assert.Equal("New Corp", response.Content.Name);
    }

    [Fact]
    public async Task CreateTenant_Returns409_WhenKeycloakThrowsConflict()
    {
        fixture.FakeKeycloak.ExceptionToThrow =
            new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.CreateTenantAsync(
            new CreateTenantRequest { CompanyName = "Dup Corp", AdminEmail = "dup@corp.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetTenantUsers_Returns200_ForKnownTenant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.GetTenantUsersAsync(tenant.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
    }

    [Fact]
    public async Task GetTenantUsers_Returns404_ForUnknownTenant()
    {
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.GetTenantUsersAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddTenantUser_Returns201_ForKnownTenant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.AddTenantUserAsync(
            tenant.Id,
            new AddTenantUserRequest { Email = "newuser@alpha.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("newuser@alpha.com", response.Content!.Email);
    }

    [Fact]
    public async Task AddTenantUser_Returns404_ForUnknownTenant()
    {
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.AddTenantUserAsync(
            Guid.NewGuid(),
            new AddTenantUserRequest { Email = "user@nowhere.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddTenantUser_Returns409_WhenKeycloakThrowsConflict()
    {
        fixture.FakeKeycloak.ExceptionToThrow =
            new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.AddTenantUserAsync(
            tenant.Id,
            new AddTenantUserRequest { Email = "existing@alpha.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTenant_Returns200_ForKnownTenant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.UpdateTenantAsync(
            tenant.Id,
            new UpdateTenantRequest { Name = "Alpha Renamed", IsActive = true },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Alpha Renamed", response.Content!.Name);
    }

    [Fact]
    public async Task UpdateTenant_Returns404_ForUnknownTenant()
    {
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.UpdateTenantAsync(
            Guid.NewGuid(),
            new UpdateTenantRequest { Name = "Ghost", IsActive = true },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateTenant_Returns200_ForKnownTenant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.DeactivateTenantAsync(tenant.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Content!.IsActive);
    }

    [Fact]
    public async Task DeactivateTenant_Returns404_ForUnknownTenant()
    {
        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.DeactivateTenantAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReactivateTenant_Returns200_ForInactiveTenant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);
        tenant.IsActive = false;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.ReactivateTenantAsync(tenant.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Content!.IsActive);
    }

    [Fact]
    public async Task EnableTenantUser_Returns204_WhenSucceeds()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.EnableTenantUserAsync(tenant.Id, "some-user-id", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task EnableTenantUser_Returns404_WhenUserNotFound()
    {
        fixture.FakeKeycloak.SetTenantUserEnabledResult = false;

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.EnableTenantUserAsync(tenant.Id, "missing-user", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DisableTenantUser_Returns204_WhenSucceeds()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        var response = await api.DisableTenantUserAsync(tenant.Id, "some-user-id", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
