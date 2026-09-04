using Heum.Data.Models;
using Heum.Server.Features.Plans.Services;
using Heum.Server.Features.Subscriptions.Models;
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

        group.MapGet("/plan/history", GetMySubscriptionHistoryAsync)
            .WithName("GetMySubscriptionHistory")
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

    static async Task<Results<Ok<List<SubscriptionResponse>>, BadRequest<ProblemDetails>>> GetMySubscriptionHistoryAsync(
        ITenantContext tenantContext,
        ISubscriptionService subscriptionService,
        CancellationToken ct)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var history = await subscriptionService.GetSubscriptionHistoryAsync(tenantContext.TenantId, ct);
        return TypedResults.Ok(history.Select(ToResponse).ToList());
    }

    static SubscriptionResponse ToResponse(TenantSubscription s) => new()
    {
        Id = s.Id,
        TenantId = s.TenantId,
        PlanId = s.PlanId,
        PlanName = s.Plan.Name,
        Reason = s.Reason.ToString(),
        Notes = s.Notes,
        ChangedByUserId = s.ChangedByUserId,
        EffectiveAtUtc = s.EffectiveAtUtc,
        CreatedAtUtc = s.CreatedAtUtc,
    };
}

public sealed class MyPlanResponse
{
    public Guid? PlanId { get; init; }
    public string? PlanName { get; init; }
    public Dictionary<string, string> Entitlements { get; init; } = [];
}
