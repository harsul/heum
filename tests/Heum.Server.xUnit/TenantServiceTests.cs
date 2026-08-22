using System.Net;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Server.Services;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;

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
        _service = new TenantService(_db, _keycloak, _events, TimeProvider.System);
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
    public async Task ProvisionTenantAsync_GeneratesExpectedSlug(string companyName, string expectedSlug)
    {
        var result = await _service.ProvisionTenantAsync(companyName, "admin@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.Equal(expectedSlug, result.Tenant!.Slug);
    }

    [Fact]
    public async Task ProvisionTenantAsync_GeneratesFallbackSlug_WhenNameIsAllSpecialChars()
    {
        var result = await _service.ProvisionTenantAsync("!!!###", "admin@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.Equal("tenant", result.Tenant!.Slug);
    }

    [Fact]
    public async Task ProvisionTenantAsync_AppendsNumericSuffix_OnSlugCollision()
    {
        await _service.ProvisionTenantAsync("Acme Corp", "first@acme.com", TestContext.Current.CancellationToken);
        var result = await _service.ProvisionTenantAsync("Acme Corp", "second@acme.com", TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.Equal("acme-corp-2", result.Tenant!.Slug);
    }

    [Fact]
    public async Task ProvisionTenantAsync_HandlesVeryLongCompanyName()
    {
        var longName = new string('a', 200);

        var result = await _service.ProvisionTenantAsync(longName, "admin@long.com", TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.True(result.Tenant!.Slug.Length <= 100);
    }

    // --- Provisioning happy path ---

    [Fact]
    public async Task ProvisionTenantAsync_CreatesTenant()
    {
        var result = await _service.ProvisionTenantAsync("My Corp", "admin@mycorp.com", TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.NotNull(result.Tenant);
        Assert.NotEqual(Guid.Empty, result.Tenant.Id);
        Assert.Equal("My Corp", result.Tenant.Name);
        Assert.True(result.Tenant.IsActive);
        Assert.Equal(1, await _db.Tenants.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProvisionTenantAsync_CallsKeycloak_WithAdminFlag()
    {
        await _service.ProvisionTenantAsync("Corp", "admin@corp.com", TestContext.Current.CancellationToken);

        Assert.Equal(1, _keycloak.CreateTenantUserCallCount);
        Assert.True(_keycloak.LastIsTenantAdmin);
    }

    [Fact]
    public async Task ProvisionTenantAsync_EnqueuesUserOnboardingEvent()
    {
        var result = await _service.ProvisionTenantAsync("Corp", "admin@corp.com", TestContext.Current.CancellationToken);

        var events = _events.EnqueuedEvents;
        var onboarding = Assert.Single(events.OfType<UserOnboardingRequestedEvent>());
        Assert.Equal(result.Tenant!.Id, onboarding.TenantId);
        Assert.Equal("admin@corp.com", onboarding.Email);
    }

    // --- Email conflict rollback ---

    [Fact]
    public async Task ProvisionTenantAsync_ReturnsEmailConflict_AndRollsBack_WhenKeycloakThrowsConflict()
    {
        _keycloak.ExceptionToThrow = new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var result = await _service.ProvisionTenantAsync("Dup Corp", "dup@corp.com", TestContext.Current.CancellationToken);

        Assert.True(result.EmailConflict);
        Assert.Null(result.Tenant);
        Assert.Equal(0, await _db.Tenants.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProvisionTenantAsync_DoesNotEnqueueEvents_OnEmailConflict()
    {
        _keycloak.ExceptionToThrow = new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        await _service.ProvisionTenantAsync("Dup Corp", "dup@corp.com", TestContext.Current.CancellationToken);

        Assert.Empty(_events.EnqueuedEvents);
    }

    // --- AddTenantUserAsync ---

    [Fact]
    public async Task AddTenantUserAsync_CallsKeycloak_WithoutAdminFlag()
    {
        var provision = await _service.ProvisionTenantAsync("Corp", "admin@corp.com", TestContext.Current.CancellationToken);
        _keycloak.Reset();

        await _service.AddTenantUserAsync(provision.Tenant!.Id, "teammate@corp.com", TestContext.Current.CancellationToken);

        Assert.Equal(1, _keycloak.CreateTenantUserCallCount);
        Assert.False(_keycloak.LastIsTenantAdmin);
    }

    [Fact]
    public async Task AddTenantUserAsync_ReturnsKeycloakUserId_OnSuccess()
    {
        var provision = await _service.ProvisionTenantAsync("Corp", "admin@corp.com", TestContext.Current.CancellationToken);
        _keycloak.UserIdToReturn = "kc-new-user";

        var result = await _service.AddTenantUserAsync(provision.Tenant!.Id, "mate@corp.com", TestContext.Current.CancellationToken);

        Assert.False(result.EmailConflict);
        Assert.Equal("kc-new-user", result.KeycloakUserId);
    }

    [Fact]
    public async Task AddTenantUserAsync_ReturnsEmailConflict_WhenKeycloakThrowsConflict()
    {
        var provision = await _service.ProvisionTenantAsync("Corp", "admin@corp.com", TestContext.Current.CancellationToken);
        _keycloak.ExceptionToThrow = new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var result = await _service.AddTenantUserAsync(provision.Tenant!.Id, "existing@corp.com", TestContext.Current.CancellationToken);

        Assert.True(result.EmailConflict);
        Assert.Null(result.KeycloakUserId);
    }
}
