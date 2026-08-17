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
    public async Task ProvisionTenantAsync_ReturnsSlugConflict_WhenSlugAlreadyExists()
    {
        await using var db = CreateDbContext();
        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var keycloak = new FakeKeycloakService();
        var tenantService = CreateService(db, keycloak);

        var result = await tenantService.ProvisionTenantAsync(
            "Acme Again", "acme", "Jane", "Doe", "jane@acme.com", "Password123",
            TestContext.Current.CancellationToken);

        Assert.True(result.SlugConflict);
        Assert.Null(result.Tenant);
        Assert.Equal(0, keycloak.ProvisionTenantAdminUserCallCount);
    }

    [Fact]
    public async Task ProvisionTenantAsync_CreatesTenantAndPublishesEvent_WhenSuccessful()
    {
        await using var db = CreateDbContext();
        var keycloak = new FakeKeycloakService { UserIdToReturn = "keycloak-user-1" };
        var events = new FakeEventPublisher();
        var tenantService = CreateService(db, keycloak, events);

        var result = await tenantService.ProvisionTenantAsync(
            "Acme", "acme", "Jane", "Doe", "jane@acme.com", "Password123",
            TestContext.Current.CancellationToken);

        Assert.False(result.SlugConflict);
        Assert.NotNull(result.Tenant);
        Assert.Equal("keycloak-user-1", result.KeycloakUserId);
        Assert.Equal(1, await db.Tenants.CountAsync(TestContext.Current.CancellationToken));

        var publishedEvent = Assert.IsType<TenantCreatedEvent>(Assert.Single(events.PublishedEvents));
        Assert.Equal(result.Tenant.Id, publishedEvent.TenantId);
        Assert.Equal("keycloak-user-1", publishedEvent.KeycloakUserId);
    }

    [Fact]
    public async Task ProvisionTenantAsync_RollsBackTenant_WhenKeycloakProvisioningFails()
    {
        await using var db = CreateDbContext();
        var keycloak = new FakeKeycloakService { ExceptionToThrow = new InvalidOperationException("Keycloak is down") };
        var events = new FakeEventPublisher();
        var tenantService = CreateService(db, keycloak, events);

        await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.ProvisionTenantAsync(
            "Acme", "acme", "Jane", "Doe", "jane@acme.com", "Password123",
            TestContext.Current.CancellationToken));

        Assert.Equal(0, await db.Tenants.CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(events.PublishedEvents);
    }
}
