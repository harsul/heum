using System.Net;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Heum.Server.xIntegration.Tests;

[Collection(nameof(IntegrationCollection))]
public class TenantAdminEndpointTests(IntegrationFixture fixture) : IAsyncLifetime
{
    private Tenant _tenant = default!;

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
        var api = RestService.For<ITenantsApi>(fixture.CreateTenantAdminClient(_tenant.Id));

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(_tenant.Id, response.Content!.Id);
        Assert.Equal("My Tenant", response.Content.Name);
    }

    [Fact]
    public async Task GetMyTenant_Returns401_WithoutToken()
    {
        var api = RestService.For<ITenantsApi>(fixture.CreateClient());

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns403_ForSystemAdminRole()
    {
        // SystemAdmin has "SystemAdmin" role but not "Admin", so TenantAdmin policy denies it
        var api = RestService.For<ITenantsApi>(fixture.CreateSystemAdminClient());

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns400_WhenTokenHasNoTenantIdClaim()
    {
        // Roles present (passes TenantAdmin policy) but no X-Test-Tenant-Id header
        // → TryGetTenantId finds no tenant_id claim → endpoint returns 400
        var api = RestService.For<ITenantsApi>(
            fixture.CreateAuthenticatedClient(roles: "Admin,User"));

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns404_WhenTenantNotInDatabase()
    {
        var api = RestService.For<ITenantsApi>(fixture.CreateTenantAdminClient(Guid.NewGuid()));

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
