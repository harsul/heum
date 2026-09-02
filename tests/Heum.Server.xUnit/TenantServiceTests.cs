using System.Net;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Server.Services;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;
using TenantService = Heum.Server.Features.Tenants.Services.TenantService;

namespace Heum.Server.xUnit;

public sealed class TenantServiceTests : IDisposable
{
    private readonly HeumDbContext _db;
    private readonly FakeKeycloakService _keycloak = new();
    private readonly FakeDomainEventCollector _events = new();
    private readonly TenantService _service;

    public TenantServiceTests()
    {
        var options = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HeumDbContext(options);
        _service = new TenantService(_db, _keycloak, _events, new FakeSubscriptionService(), TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    // --- Slug generation ---

    [Theory]
    [InlineData("Acme Corp", "acme-corp")]
    [InlineData("C&A Industries!", "c-a-industries")]
    [InlineData("UPPER CASE", "upper-case")]
    [InlineData("  leading and trailing  ", "leading-and-trailing")]
    [InlineData("multiple---dashes", "multiple-dashes")]
    [InlineData("123 Numbers", "123-numbers")]
    public async Task CreateTenantAsync_GeneratesExpectedSlug(string companyName, string expectedSlug)
    {
        var tenant = await _service.CreateTenantAsync(companyName, TestContext.Current.CancellationToken);

        Assert.Equal(expectedSlug, tenant.Slug);
    }

    [Fact]
    public async Task CreateTenantAsync_GeneratesFallbackSlug_WhenNameIsAllSpecialChars()
    {
        var tenant = await _service.CreateTenantAsync("!!!###", TestContext.Current.CancellationToken);

        Assert.Equal("tenant", tenant.Slug);
    }

    [Fact]
    public async Task CreateTenantAsync_AppendsNumericSuffix_OnSlugCollision()
    {
        await _service.CreateTenantAsync("Acme Corp", TestContext.Current.CancellationToken);
        var tenant = await _service.CreateTenantAsync("Acme Corp", TestContext.Current.CancellationToken);

        Assert.Equal("acme-corp-2", tenant.Slug);
    }

    [Fact]
    public async Task CreateTenantAsync_HandlesVeryLongCompanyName()
    {
        var longName = new string('a', 200);

        var tenant = await _service.CreateTenantAsync(longName, TestContext.Current.CancellationToken);

        Assert.True(tenant.Slug.Length <= 100);
    }

    // --- Creation happy path ---

    [Fact]
    public async Task CreateTenantAsync_CreatesTenant()
    {
        var tenant = await _service.CreateTenantAsync("My Corp", TestContext.Current.CancellationToken);

        Assert.NotNull(tenant);
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("My Corp", tenant.Name);
        Assert.True(tenant.IsActive);
        Assert.Equal(1, await _db.Tenants.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateTenantAsync_DoesNotCallKeycloak()
    {
        await _service.CreateTenantAsync("Corp", TestContext.Current.CancellationToken);

        Assert.Equal(0, _keycloak.CreateTenantUserCallCount);
    }

    // --- AddTenantUserAsync ---

    [Fact]
    public async Task AddTenantUserAsync_CallsKeycloak_WithoutAdminFlag()
    {
        var tenant = await _service.CreateTenantAsync("Corp", TestContext.Current.CancellationToken);
        _keycloak.Reset();

        await _service.AddTenantUserAsync(tenant.Id, "teammate@corp.com", role: null, TestContext.Current.CancellationToken);

        Assert.Equal(1, _keycloak.CreateTenantUserCallCount);
        Assert.Null(_keycloak.LastRole);
    }

    [Fact]
    public async Task AddTenantUserAsync_ReturnsKeycloakUserId_OnSuccess()
    {
        var tenant = await _service.CreateTenantAsync("Corp", TestContext.Current.CancellationToken);
        _keycloak.UserIdToReturn = "kc-new-user";

        var result = await _service.AddTenantUserAsync(tenant.Id, "mate@corp.com", role: null, TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.Equal("kc-new-user", result.KeycloakUserId);
    }

    [Fact]
    public async Task AddTenantUserAsync_ReturnsEmailConflict_WhenKeycloakThrowsConflict()
    {
        var tenant = await _service.CreateTenantAsync("Corp", TestContext.Current.CancellationToken);
        _keycloak.ExceptionToThrow = new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var result = await _service.AddTenantUserAsync(tenant.Id, "existing@corp.com", role: null, TestContext.Current.CancellationToken);

        Assert.True(result.EmailConflict);
        Assert.Null(result.KeycloakUserId);
    }
}
