using Heum.Server.Features.Plans.Services;
using Heum.Server.Features.Subscriptions.Services;
using Heum.Server.Features.Tenants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Subscriptions.Endpoints;

public static class TenantEntitlementsEndpoints
{
    public static RouteGroupBuilder MapTenantEntitlementsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/plan", GetMyPlanAsync)
            .WithName("GetMyPlan")
            .RequireAuthorization("TenantAdmin");

        return group;
    }

    static async Task<Results<Ok<MyPlanResponse>, BadRequest<ProblemDetails>>> GetMyPlanAsync(
        ITenantContext tenantContext,
        ISubscriptionService subscriptionService,
        CancellationToken ct)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var tenantId = tenantContext.TenantId;
        var sub = await subscriptionService.GetCurrentSubscriptionAsync(tenantId, ct);
        var entitlements = await subscriptionService.GetResolvedEntitlementsAsync(tenantId, ct);

        return TypedResults.Ok(new MyPlanResponse
        {
            PlanId = sub?.PlanId,
            PlanName = sub?.Plan.Name,
            Entitlements = entitlements.ToDictionary(),
        });
    }
}

public sealed class MyPlanResponse
{
    public Guid? PlanId { get; init; }
    public string? PlanName { get; init; }
    public Dictionary<string, string> Entitlements { get; init; } = [];
}
