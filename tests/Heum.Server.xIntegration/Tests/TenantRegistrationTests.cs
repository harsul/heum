using System.Net;
using Heum.BackgroundService;
using Heum.BackgroundService.Outbox;
using Heum.Data;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.Services;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heum.Server.xIntegration.Tests;

[Collection(nameof(IntegrationCollection))]
public class TenantRegistrationTests(IntegrationFixture fixture) : IAsyncLifetime
{
    async ValueTask IAsyncLifetime.InitializeAsync() =>
        await fixture.ResetDatabaseAsync();

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RegisterTenant_Returns201_WithValidRequest()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.Anonymous);

        var response = await api.RegisterTenantAsync(
            new CreateTenantRequest { CompanyName = "Acme Corp", AdminEmail = "admin@acme.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotEqual(Guid.Empty, response.Content!.TenantId);
        Assert.Equal("acme-corp", response.Content.Slug);
        Assert.Equal(fixture.FakeKeycloak.UserIdToReturn, response.Content.KeycloakUserId);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = await db.Tenants.SingleOrDefaultAsync(
            t => t.Id == response.Content.TenantId, TestContext.Current.CancellationToken);
        Assert.NotNull(tenant);
        Assert.Equal("Acme Corp", tenant.Name);
        Assert.True(tenant.IsActive);

        // ProvisionTenantAsync raises TenantCreatedEvent + UserOnboardingRequestedEvent, which
        // land in the OutboxMessages table transactionally - nothing has been published yet.
        Assert.Equal(2, await db.OutboxMessages.CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(fixture.FakeEvents.PublishedEvents);

        // Explicitly drain the outbox instead of waiting on the poll interval.
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, fixture.FakeEvents.PublishedEvents.Count);
        Assert.True(await db.OutboxMessages.AllAsync(
            m => m.ProcessedAtUtc != null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RegisterTenant_Returns409_WhenKeycloakThrowsConflict()
    {
        fixture.FakeKeycloak.ExceptionToThrow =
            new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var api = fixture.GetClient<ITenantsApi>(ClientScope.Anonymous);

        var response = await api.RegisterTenantAsync(
            new CreateTenantRequest { CompanyName = "Duplicate Corp", AdminEmail = "duplicate@corp.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Tenant row is rolled back when email conflicts, and no domain events were ever raised
        // (MarkProvisioned/the onboarding event are only queued after Keycloak succeeds), so
        // nothing should have landed in the outbox either.
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        Assert.False(await db.Tenants.AnyAsync(TestContext.Current.CancellationToken));
        Assert.False(await db.OutboxMessages.AnyAsync(TestContext.Current.CancellationToken));
        Assert.Empty(fixture.FakeEvents.PublishedEvents);
    }

    [Fact]
    public async Task RegisterTenant_Returns400_WithInvalidRequest()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.Anonymous);

        var response = await api.RegisterTenantAsync(
            new CreateTenantRequest { CompanyName = "", AdminEmail = "admin@acme.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterTenant_IsAccessibleWithoutAuth()
    {
        var api = fixture.GetClient<ITenantsApi>(ClientScope.Anonymous);

        var response = await api.RegisterTenantAsync(
            new CreateTenantRequest { CompanyName = "Open Corp", AdminEmail = "open@corp.com" },
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
