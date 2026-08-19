using System.Net;
using System.Net.Http.Json;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

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
        var response = await fixture.CreateTenantAdminClient(_tenant.Id)
            .GetAsync("/api/tenants/me/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TenantResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(_tenant.Id, body.Id);
        Assert.Equal("My Tenant", body.Name);
    }

    [Fact]
    public async Task GetMyTenant_Returns401_WithoutToken()
    {
        var response = await fixture.CreateClient()
            .GetAsync("/api/tenants/me/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns403_ForSystemAdminRole()
    {
        // SystemAdmin has "SystemAdmin" role but not "Admin", so TenantAdmin policy denies it
        var response = await fixture.CreateSystemAdminClient()
            .GetAsync("/api/tenants/me/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns400_WhenTokenHasNoTenantIdClaim()
    {
        // Token has "Admin" role but no tenant_id claim — TryGetTenantId returns false → 400
        var token = JwtTokenFactory.CreateToken(
            subject: "admin-without-tenant",
            tenantId: null,
            realmRoles: ["Admin", "User"]);

        var response = await fixture.CreateAuthenticatedClient(token)
            .GetAsync("/api/tenants/me/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTenant_Returns404_WhenTenantNotInDatabase()
    {
        var response = await fixture.CreateTenantAdminClient(Guid.NewGuid())
            .GetAsync("/api/tenants/me/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
