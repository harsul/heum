using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Subscriptions.Services;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heum.Server.xUnit;

public sealed class SubscriptionServiceTests : IDisposable
{
    private readonly HeumDbContext _db;
    private readonly FakeDomainEventCollector _events = new();
    private readonly FakeEntitlementService _entitlements = new();
    private readonly SubscriptionService _service;

    public SubscriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new HeumDbContext(options);
        _service = new SubscriptionService(_db, _entitlements, _events, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Tenant> SeedTenantAsync()
    {
        var tenant = Tenant.Register("Test Corp", "test-corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return tenant;
    }

    private async Task<Plan> SeedPlanAsync(bool isActive = true)
    {
        var plan = Plan.Create("Starter", TimeProvider.System);
        if (!isActive) plan.SetActive(false, TimeProvider.System);
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return plan;
    }

    [Fact]
    public async Task AssignPlanAsync_ReturnsTenantNotFound_WhenMissing()
    {
        var plan = await SeedPlanAsync();

        var result = await _service.AssignPlanAsync(Guid.NewGuid(), plan.Id, null, null, TestContext.Current.CancellationToken);

        Assert.Equal(AssignPlanFailure.TenantNotFound, result.Failure);
    }

    [Fact]
    public async Task AssignPlanAsync_ReturnsPlanNotFound_WhenMissing()
    {
        var tenant = await SeedTenantAsync();

        var result = await _service.AssignPlanAsync(tenant.Id, Guid.NewGuid(), null, null, TestContext.Current.CancellationToken);

        Assert.Equal(AssignPlanFailure.PlanNotFound, result.Failure);
    }

    [Fact]
    public async Task AssignPlanAsync_ReturnsPlanInactive_WhenPlanDeactivated()
    {
        var tenant = await SeedTenantAsync();
        var plan = await SeedPlanAsync(isActive: false);

        var result = await _service.AssignPlanAsync(tenant.Id, plan.Id, null, null, TestContext.Current.CancellationToken);

        Assert.Equal(AssignPlanFailure.PlanInactive, result.Failure);
    }

    [Fact]
    public async Task AssignPlanAsync_AssignsPlan_WhenValid()
    {
        var tenant = await SeedTenantAsync();
        var plan = await SeedPlanAsync();

        var result = await _service.AssignPlanAsync(tenant.Id, plan.Id, "Initial setup", "admin-user", TestContext.Current.CancellationToken);

        Assert.NotNull(result.Subscription);
        Assert.Equal(AssignPlanFailure.None, result.Failure);
        Assert.Equal(tenant.Id, result.Subscription.TenantId);
        Assert.Equal(plan.Id, result.Subscription.PlanId);
    }

    [Fact]
    public async Task AssignPlanAsync_SetsReasonToInitial_WhenFirstAssignment()
    {
        var tenant = await SeedTenantAsync();
        var plan = await SeedPlanAsync();

        var result = await _service.AssignPlanAsync(tenant.Id, plan.Id, null, null, TestContext.Current.CancellationToken);

        Assert.Equal(SubscriptionChangeReason.Initial, result.Subscription!.Reason);
    }

    [Fact]
    public async Task AssignPlanAsync_SetsReasonToAdminOverride_WhenAlreadyHasPlan()
    {
        var tenant = await SeedTenantAsync();
        var plan1 = await SeedPlanAsync();
        var plan2 = Plan.Create("Pro", TimeProvider.System);
        _db.Plans.Add(plan2);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.AssignPlanAsync(tenant.Id, plan1.Id, null, null, TestContext.Current.CancellationToken);
        var result = await _service.AssignPlanAsync(tenant.Id, plan2.Id, null, null, TestContext.Current.CancellationToken);

        Assert.Equal(SubscriptionChangeReason.AdminOverride, result.Subscription!.Reason);
    }

    [Fact]
    public async Task GetCurrentSubscriptionAsync_ReturnsNull_WhenNoSubscription()
    {
        var result = await _service.GetCurrentSubscriptionAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSubscriptionHistoryAsync_ReturnsAllEntries()
    {
        var tenant = await SeedTenantAsync();
        var plan = await SeedPlanAsync();
        await _service.AssignPlanAsync(tenant.Id, plan.Id, null, null, TestContext.Current.CancellationToken);

        var history = await _service.GetSubscriptionHistoryAsync(tenant.Id, TestContext.Current.CancellationToken);

        Assert.Single(history);
    }
}
