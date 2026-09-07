using Heum.Data;
using Heum.Data.Auditing;
using Heum.Data.Models;
using Heum.Server.Features.Tenants.Services;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TenantService = Heum.Server.Features.Tenants.Services.TenantService;

namespace Heum.Server.xUnit;

public sealed class TenantServiceAdditionalTests : IDisposable
{
    private readonly HeumDbContext _db;
    private readonly FakeKeycloakService _keycloak = new();
    private readonly FakeDomainEventCollector _events = new();
    private readonly FakeTenantStatusService _tenantStatus = new();
    private readonly TenantService _service;

    public TenantServiceAdditionalTests()
    {
        var options = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new HeumDbContext(options);
        _service = new TenantService(_db, _keycloak, _events, new FakeSubscriptionService(), _tenantStatus, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ListTenantsAsync_WithSearch_FiltersResults()
    {
        await _service.CreateTenantAsync("Alpha Corp", TestContext.Current.CancellationToken);
        await _service.CreateTenantAsync("Beta Inc", TestContext.Current.CancellationToken);

        var (items, total) = await _service.ListTenantsAsync("Alpha", null, null, 1, 10, TestContext.Current.CancellationToken);

        Assert.Equal(1, total);
        Assert.Equal("Alpha Corp", items[0].Name);
    }

    [Fact]
    public async Task ListTenantsAsync_Paginate_ReturnsCorrectPage()
    {
        await _service.CreateTenantAsync("A Tenant", TestContext.Current.CancellationToken);
        await _service.CreateTenantAsync("B Tenant", TestContext.Current.CancellationToken);
        await _service.CreateTenantAsync("C Tenant", TestContext.Current.CancellationToken);

        var (items, total) = await _service.ListTenantsAsync(null, null, null, 2, 2, TestContext.Current.CancellationToken);

        Assert.Equal(3, total);
        Assert.Single(items);
    }

    [Fact]
    public async Task ListTenantsAsync_SortByName_Desc_ReturnsOrderedResults()
    {
        await _service.CreateTenantAsync("Alpha Corp", TestContext.Current.CancellationToken);
        await _service.CreateTenantAsync("Beta Inc", TestContext.Current.CancellationToken);

        var (items, _) = await _service.ListTenantsAsync(null, "name", "desc", 1, 10, TestContext.Current.CancellationToken);

        Assert.Equal("Beta Inc", items[0].Name);
    }

    [Fact]
    public async Task ListTenantsAsync_SortBySlug_Asc_ReturnsOrderedResults()
    {
        await _service.CreateTenantAsync("Beta Inc", TestContext.Current.CancellationToken);
        await _service.CreateTenantAsync("Alpha Corp", TestContext.Current.CancellationToken);

        var (items, _) = await _service.ListTenantsAsync(null, "slug", "asc", 1, 10, TestContext.Current.CancellationToken);

        Assert.Equal("alpha-corp", items[0].Slug);
    }

    [Fact]
    public async Task UpdateTenantAsync_RenamesSlug_WhenNameChanges()
    {
        var tenant = await _service.CreateTenantAsync("Old Name", TestContext.Current.CancellationToken);

        var updated = await _service.UpdateTenantAsync(tenant.Id, "New Name", true, TestContext.Current.CancellationToken);

        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task UpdateTenantAsync_ReturnsNull_WhenTenantNotFound()
    {
        var result = await _service.UpdateTenantAsync(Guid.NewGuid(), "Name", true, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTenantAsync_InvalidatesStatus_WhenActiveChanges()
    {
        var tenant = await _service.CreateTenantAsync("Corp", TestContext.Current.CancellationToken);

        await _service.UpdateTenantAsync(tenant.Id, "Corp", false, TestContext.Current.CancellationToken);

        Assert.Contains(tenant.Id, _tenantStatus.Invalidated);
    }

    [Fact]
    public async Task GetTenantHistoryAsync_ReturnsAuditTrailForTenant()
    {
        var tenant = await _service.CreateTenantAsync("Corp", TestContext.Current.CancellationToken);
        _db.Set<AuditTrail>().Add(new AuditTrail
        {
            EntityName = nameof(Tenant),
            PrimaryKey = tenant.Id.ToString(),
            Action = AuditAction.Update,
            TimestampUtc = DateTime.UtcNow,
            UserId = "test",
        });
        await _db.SaveChangesAsync();

        var (items, total) = await _service.GetTenantHistoryAsync(tenant.Id, 1, 10, TestContext.Current.CancellationToken);

        Assert.Equal(1, total);
        Assert.Single(items);
    }

    [Fact]
    public async Task SetLogoAsync_UpdatesLogoUrl()
    {
        var tenant = await _service.CreateTenantAsync("Corp", TestContext.Current.CancellationToken);

        var updated = await _service.SetLogoAsync(tenant.Id, "/logos/my-logo.png", TestContext.Current.CancellationToken);

        Assert.NotNull(updated);
        Assert.Equal("/logos/my-logo.png", updated.LogoUrl);
    }

    [Fact]
    public async Task SetLogoAsync_ReturnsNull_WhenTenantNotFound()
    {
        var result = await _service.SetLogoAsync(Guid.NewGuid(), "/logo.png", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddTenantUserAsync_ReturnsInvalidRole_WhenRoleNotAssignable()
    {
        var tenant = await _service.CreateTenantAsync("Corp", TestContext.Current.CancellationToken);
        _keycloak.AssignableRolesToReturn = ["Admin"];

        var result = await _service.AddTenantUserAsync(tenant.Id, "user@corp.com", "SuperAdmin", TestContext.Current.CancellationToken);

        Assert.True(result.InvalidRole);
    }
}
