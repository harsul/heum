using System.Net;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Plans.Models;
using Heum.Server.Features.Subscriptions.Models;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;

namespace Heum.Server.xIntegration.Tests;

[Collection(nameof(IntegrationCollection))]
public class AdminSubscriptionsEndpointTests(IntegrationFixture fixture) : IAsyncLifetime
{
    private Guid _tenantId;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();

        var admin = fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);
        var created = await admin.CreateTenantAsync(new CreateTenantRequest { CompanyName = "Sub Co" });
        _tenantId = created.Content!.Id;
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreateTenant_AssignsFreePlan_Atomically()
    {
        var api = fixture.GetClient<IAdminPlansApi>(ClientScope.SystemAdmin);

        var response = await api.GetCurrentSubscriptionAsync(_tenantId, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WellKnownIds.FreePlanId, response.Content!.PlanId);
        Assert.Equal(nameof(SubscriptionChangeReason.Initial), response.Content.Reason);
    }

    [Fact]
    public async Task AssignPlan_Returns404_ForUnknownTenant()
    {
        var api = fixture.GetClient<IAdminPlansApi>(ClientScope.SystemAdmin);

        var response = await api.AssignPlanAsync(
            Guid.NewGuid(),
            new AssignPlanRequest { PlanId = WellKnownIds.FreePlanId },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignPlan_Returns404_ForUnknownPlan()
    {
        var api = fixture.GetClient<IAdminPlansApi>(ClientScope.SystemAdmin);

        var response = await api.AssignPlanAsync(
            _tenantId,
            new AssignPlanRequest { PlanId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignPlan_Returns400_ForInactivePlan()
    {
        var api = fixture.GetClient<IAdminPlansApi>(ClientScope.SystemAdmin);
        var plan = await api.CreatePlanAsync(new CreatePlanRequest { Name = $"Retired-{Guid.NewGuid():N}" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, plan.StatusCode);

        var retired = await api.UpdatePlanAsync(
            plan.Content!.Id,
            new UpdatePlanRequest { Name = plan.Content.Name, IsActive = false },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, retired.StatusCode);

        var response = await api.AssignPlanAsync(
            _tenantId,
            new AssignPlanRequest { PlanId = plan.Content.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlan_Returns409_ForDuplicateName()
    {
        var api = fixture.GetClient<IAdminPlansApi>(ClientScope.SystemAdmin);
        var name = $"Pro-{Guid.NewGuid():N}";

        var first = await api.CreatePlanAsync(new CreatePlanRequest { Name = name }, TestContext.Current.CancellationToken);
        var second = await api.CreatePlanAsync(new CreatePlanRequest { Name = name }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateEntitlement_Returns409_ForDuplicateKey()
    {
        var api = fixture.GetClient<IAdminPlansApi>(ClientScope.SystemAdmin);

        var response = await api.CreateEntitlementAsync(
            new CreateEntitlementRequest { Key = EntitlementKeys.MaxUsers, Type = EntitlementType.Integer },
            TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode == HttpStatusCode.Conflict,
            $"Expected 409 but got {(int)response.StatusCode}: {(response.Error as Refit.ApiException)?.Content}");
    }

    [Fact]
    public async Task UpsertOverride_Returns404_ForUnknownEntitlementKey()
    {
        var api = fixture.GetClient<IAdminPlansApi>(ClientScope.SystemAdmin);

        var response = await api.UpsertOverrideAsync(
            _tenantId,
            "does_not_exist",
            new EntitlementOverrideRequest { Value = "1" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpsertOverride_Returns404_ForUnknownTenant()
    {
        var api = fixture.GetClient<IAdminPlansApi>(ClientScope.SystemAdmin);

        var response = await api.UpsertOverrideAsync(
            Guid.NewGuid(),
            EntitlementKeys.MaxUsers,
            new EntitlementOverrideRequest { Value = "1" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
