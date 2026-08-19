using System.Net;
using System.Net.Http.Json;
using Heum.Data;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heum.Server.xIntegration.Tests;

[Collection(nameof(IntegrationCollection))]
public class TenantRegistrationTests(IntegrationFixture fixture) : IAsyncLifetime
{
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        db.Tenants.RemoveRange(db.Tenants);
        await db.SaveChangesAsync();

        fixture.FakeEvents.Clear();
        fixture.FakeKeycloak.Reset();
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RegisterTenant_Returns201_WithValidRequest()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tenants/register", new
        {
            CompanyName = "Acme Corp",
            AdminEmail = "admin@acme.com",
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterTenantResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.TenantId);
        Assert.Equal("acme-corp", body.Slug);
        Assert.Equal(fixture.FakeKeycloak.UserIdToReturn, body.KeycloakUserId);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.SingleOrDefaultAsync(
            t => t.Id == body.TenantId, TestContext.Current.CancellationToken);
        Assert.NotNull(tenant);
        Assert.Equal("Acme Corp", tenant.Name);
        Assert.True(tenant.IsActive);

        // ProvisionTenantAsync publishes UserOnboardingRequestedEvent then TenantCreatedEvent
        Assert.Equal(2, fixture.FakeEvents.PublishedEvents.Count);
    }

    [Fact]
    public async Task RegisterTenant_Returns409_WhenKeycloakThrowsConflict()
    {
        fixture.FakeKeycloak.ExceptionToThrow =
            new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var response = await fixture.CreateClient().PostAsJsonAsync("/api/tenants/register", new
        {
            CompanyName = "Duplicate Corp",
            AdminEmail = "duplicate@corp.com",
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Tenant row is rolled back when email conflicts
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        Assert.False(await db.Tenants.AnyAsync(TestContext.Current.CancellationToken));
        Assert.Empty(fixture.FakeEvents.PublishedEvents);
    }

    [Fact]
    public async Task RegisterTenant_Returns400_WithInvalidRequest()
    {
        var response = await fixture.CreateClient().PostAsJsonAsync("/api/tenants/register", new
        {
            CompanyName = "",
            AdminEmail = "admin@acme.com",
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterTenant_IsAccessibleWithoutAuth()
    {
        // No Authorization header — endpoint is AllowAnonymous
        var response = await fixture.CreateClient().PostAsJsonAsync("/api/tenants/register", new
        {
            CompanyName = "Open Corp",
            AdminEmail = "open@corp.com",
        }, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
