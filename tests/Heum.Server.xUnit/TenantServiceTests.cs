using System.Net;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Tenants;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.xUnit;

public class TenantServiceTests
{
    private static HeumDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HeumDbContext(options);
    }

    private static TenantService CreateService(
        HeumDbContext dbContext,
        FakeKeycloakService? keycloakService = null,
        FakeEventPublisher? eventPublisher = null)
        => new(dbContext, keycloakService ?? new FakeKeycloakService(), eventPublisher ?? new FakeEventPublisher());

    [Fact]
    public async Task ListTenantsAsync_ReturnsAllTenantsOrderedByName()
    {
        await using var db = CreateDbContext();
        db.Tenants.AddRange(
            new Tenant { Id = Guid.NewGuid(), Name = "Zeta Co", Slug = "zeta" },
            new Tenant { Id = Guid.NewGuid(), Name = "Acme Co", Slug = "acme" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tenantService = CreateService(db);

        var result = await tenantService.ListTenantsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Acme Co", "Zeta Co"], result.Select(t => t.Name));
    }

    [Fact]
    public async Task GetTenantAsync_ReturnsNull_WhenTenantDoesNotExist()
    {
        await using var db = CreateDbContext();
        var tenantService = CreateService(db);

        var result = await tenantService.GetTenantAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTenantAsync_UpdatesNameAndIsActive()
    {
        await using var db = CreateDbContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Old Name", Slug = "old", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tenantService = CreateService(db);

        var result = await tenantService.UpdateTenantAsync(
            tenant.Id, "New Name", isActive: false, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.False(result.IsActive);
        Assert.NotNull(result.UpdatedAtUtc);
    }

    [Fact]
    public async Task SetTenantActiveAsync_DeactivatesTenant()
    {
        await using var db = CreateDbContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tenantService = CreateService(db);

        var result = await tenantService.SetTenantActiveAsync(tenant.Id, isActive: false, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task SetTenantActiveAsync_ReactivatesTenant()
    {
        await using var db = CreateDbContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme", IsActive = false };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tenantService = CreateService(db);

        var result = await tenantService.SetTenantActiveAsync(tenant.Id, isActive: true, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task ProvisionTenantAsync_GeneratesUniqueSlug_WhenCompanyNameCollides()
    {
        await using var db = CreateDbContext();
        var tenantService = CreateService(db);

        var first = await tenantService.ProvisionTenantAsync(
            "Acme", "jane@acme.com", TestContext.Current.CancellationToken);
        var second = await tenantService.ProvisionTenantAsync(
            "Acme", "john@acme.com", TestContext.Current.CancellationToken);

        Assert.False(first.EmailConflict);
        Assert.False(second.EmailConflict);
        Assert.Equal("acme", first.Tenant!.Slug);
        Assert.Equal("acme-2", second.Tenant!.Slug);
    }

    [Fact]
    public async Task ProvisionTenantAsync_CreatesTenantAndPublishesEvents_WhenSuccessful()
    {
        await using var db = CreateDbContext();
        var keycloak = new FakeKeycloakService { UserIdToReturn = "keycloak-user-1" };
        var events = new FakeEventPublisher();
        var tenantService = CreateService(db, keycloak, events);

        var result = await tenantService.ProvisionTenantAsync(
            "Acme", "jane@acme.com", TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.NotNull(result.Tenant);
        Assert.Equal("acme", result.Tenant.Slug);
        Assert.Equal("keycloak-user-1", result.KeycloakUserId);
        Assert.Equal(1, await db.Tenants.CountAsync(TestContext.Current.CancellationToken));

        Assert.Equal(2, events.PublishedEvents.Count);

        var tenantCreated = Assert.IsType<TenantCreatedEvent>(
            events.PublishedEvents.Single(e => e is TenantCreatedEvent));
        Assert.Equal(result.Tenant.Id, tenantCreated.TenantId);
        Assert.Equal("keycloak-user-1", tenantCreated.KeycloakUserId);

        var onboardingRequested = Assert.IsType<UserOnboardingRequestedEvent>(
            events.PublishedEvents.Single(e => e is UserOnboardingRequestedEvent));
        Assert.Equal(result.Tenant.Id, onboardingRequested.TenantId);
        Assert.Equal("jane@acme.com", onboardingRequested.Email);
        Assert.Equal("keycloak-user-1", onboardingRequested.KeycloakUserId);
    }

    [Fact]
    public async Task ProvisionTenantAsync_ReturnsEmailConflict_WhenKeycloakEmailAlreadyExists()
    {
        await using var db = CreateDbContext();
        var keycloak = new FakeKeycloakService
        {
            ExceptionToThrow = new HttpRequestException("Conflict", null, HttpStatusCode.Conflict),
        };
        var events = new FakeEventPublisher();
        var tenantService = CreateService(db, keycloak, events);

        var result = await tenantService.ProvisionTenantAsync(
            "Acme", "jane@acme.com", TestContext.Current.CancellationToken);

        Assert.True(result.EmailConflict);
        Assert.Null(result.Tenant);
        Assert.Equal(0, await db.Tenants.CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(events.PublishedEvents);
    }

    [Fact]
    public async Task ProvisionTenantAsync_RollsBackTenant_WhenKeycloakProvisioningFails()
    {
        await using var db = CreateDbContext();
        var keycloak = new FakeKeycloakService { ExceptionToThrow = new InvalidOperationException("Keycloak is down") };
        var events = new FakeEventPublisher();
        var tenantService = CreateService(db, keycloak, events);

        await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.ProvisionTenantAsync(
            "Acme", "jane@acme.com", TestContext.Current.CancellationToken));

        Assert.Equal(0, await db.Tenants.CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(events.PublishedEvents);
    }

    [Fact]
    public async Task AddTenantUserAsync_PublishesOnboardingEvent_WhenSuccessful()
    {
        await using var db = CreateDbContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var keycloak = new FakeKeycloakService { UserIdToReturn = "keycloak-user-2" };
        var events = new FakeEventPublisher();
        var tenantService = CreateService(db, keycloak, events);

        var result = await tenantService.AddTenantUserAsync(
            tenant.Id, "teammate@acme.com", TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.Equal("keycloak-user-2", result.KeycloakUserId);

        var onboardingRequested = Assert.IsType<UserOnboardingRequestedEvent>(Assert.Single(events.PublishedEvents));
        Assert.Equal(tenant.Id, onboardingRequested.TenantId);
        Assert.Equal("teammate@acme.com", onboardingRequested.Email);
        Assert.Equal("keycloak-user-2", onboardingRequested.KeycloakUserId);
    }

    [Fact]
    public async Task AddTenantUserAsync_ReturnsEmailConflict_WhenKeycloakEmailAlreadyExists()
    {
        await using var db = CreateDbContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var keycloak = new FakeKeycloakService
        {
            ExceptionToThrow = new HttpRequestException("Conflict", null, HttpStatusCode.Conflict),
        };
        var events = new FakeEventPublisher();
        var tenantService = CreateService(db, keycloak, events);

        var result = await tenantService.AddTenantUserAsync(
            tenant.Id, "teammate@acme.com", TestContext.Current.CancellationToken);

        Assert.True(result.EmailConflict);
        Assert.Null(result.KeycloakUserId);
        Assert.Empty(events.PublishedEvents);
    }
}
