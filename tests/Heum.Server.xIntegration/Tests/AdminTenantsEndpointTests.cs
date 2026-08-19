using System.Net;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refit;

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
        var api = RestService.For<IAdminTenantsApi>(fixture.CreateSystemAdminClient());

        var response = await api.ListTenantsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, response.Content!.Count);
    }

    [Fact]
    public async Task ListTenants_Returns401_WithoutToken()
    {
        var api = RestService.For<IAdminTenantsApi>(fixture.CreateClient());

        var response = await api.ListTenantsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListTenants_Returns403_ForTenantAdminRole()
    {
        // TenantAdmin has "Admin" role but /api/admin/tenants requires "SystemAdmin"
        var api = RestService.For<IAdminTenantsApi>(fixture.CreateTenantAdminClient(Guid.NewGuid()));

        var response = await api.ListTenantsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTenant_Returns200_ForExistingTenant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var api = RestService.For<IAdminTenantsApi>(fixture.CreateSystemAdminClient());

        var response = await api.GetTenantAsync(tenant.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(tenant.Id, response.Content!.Id);
    }

    [Fact]
    public async Task GetTenant_Returns404_ForUnknownId()
    {
        var api = RestService.For<IAdminTenantsApi>(fixture.CreateSystemAdminClient());

        var response = await api.GetTenantAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
