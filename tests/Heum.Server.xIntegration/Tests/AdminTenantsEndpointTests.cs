using System.Net;
using System.Net.Http.Json;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Tenants.Models;
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
        var response = await fixture.CreateSystemAdminClient()
            .GetAsync("/api/admin/tenants/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<TenantResponse>>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    public async Task ListTenants_Returns401_WithoutToken()
    {
        var response = await fixture.CreateClient()
            .GetAsync("/api/admin/tenants/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListTenants_Returns403_ForTenantAdminRole()
    {
        // TenantAdmin has "Admin" role but /api/admin/tenants requires "SystemAdmin"
        var response = await fixture.CreateTenantAdminClient(Guid.NewGuid())
            .GetAsync("/api/admin/tenants/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTenant_Returns200_ForExistingTenant()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.FirstAsync(TestContext.Current.CancellationToken);

        var response = await fixture.CreateSystemAdminClient()
            .GetAsync($"/api/admin/tenants/{tenant.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTenant_Returns404_ForUnknownId()
    {
        var response = await fixture.CreateSystemAdminClient()
            .GetAsync($"/api/admin/tenants/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
