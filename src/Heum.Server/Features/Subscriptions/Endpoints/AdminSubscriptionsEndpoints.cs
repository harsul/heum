using System.Security.Claims;
using Heum.Data.Models;
using Heum.Server.Features.Plans;
using Heum.Server.Features.Subscriptions.Models;
using Heum.Server.Features.Subscriptions.Services;
using Heum.Server.Features.Tenants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Subscriptions.Endpoints;

public static class AdminSubscriptionsEndpoints
{
    public static RouteGroupBuilder MapAdminSubscriptionsEndpoints(this RouteGroupBuilder group)
    {
        // Mapped directly on the /admin group, so the routes are /api/admin/{tenantId}/subscription
        // and /api/admin/{tenantId}/entitlements (the frontend's plansApi.ts relies on this shape).
        group.MapGet("/{id:guid}/subscription", GetCurrentSubscriptionAsync).WithName("GetCurrentSubscription");
        group.MapPost("/{id:guid}/subscription", AssignPlanAsync).WithName("AssignPlan");
        group.MapGet("/{id:guid}/subscription/history", GetSubscriptionHistoryAsync).WithName("GetSubscriptionHistory");

        // Entitlement override management
        group.MapGet("/{id:guid}/entitlements", GetResolvedEntitlementsAsync).WithName("GetResolvedEntitlements");
        group.MapPut("/{id:guid}/entitlements/{key}", UpsertOverrideAsync).WithName("UpsertEntitlementOverride");
        group.MapDelete("/{id:guid}/entitlements/{key}", RemoveOverrideAsync).WithName("RemoveEntitlementOverride");

        return group;
    }

    static async Task<Results<Ok<SubscriptionResponse>, NotFound>> GetCurrentSubscriptionAsync(
        Guid id, ISubscriptionService service, CancellationToken ct)
    {
        var sub = await service.GetCurrentSubscriptionAsync(id, ct);
        return sub is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(sub));
    }

    static async Task<Results<Ok<SubscriptionResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> AssignPlanAsync(
        Guid id, AssignPlanRequest request, ISubscriptionService service,
        ClaimsPrincipal user, CancellationToken ct)
    {
        var changedBy = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        var result = await service.AssignPlanAsync(id, request.PlanId, request.Notes, changedBy, ct);

        return result.Failure switch
        {
            AssignPlanFailure.TenantNotFound => TypedResults.NotFound(TenantProblems.TenantNotFound(id)),
            AssignPlanFailure.PlanNotFound => TypedResults.NotFound(PlanProblems.PlanNotFound(request.PlanId)),
            AssignPlanFailure.PlanInactive => TypedResults.BadRequest(PlanProblems.PlanInactive(request.PlanId)),
            _ => TypedResults.Ok(ToResponse(result.Subscription!)),
        };
    }

    static async Task<Ok<List<SubscriptionResponse>>> GetSubscriptionHistoryAsync(
        Guid id, ISubscriptionService service, CancellationToken ct)
    {
        var history = await service.GetSubscriptionHistoryAsync(id, ct);
        return TypedResults.Ok(history.Select(ToResponse).ToList());
    }

    static async Task<Ok<Dictionary<string, string>>> GetResolvedEntitlementsAsync(
        Guid id, ISubscriptionService service, CancellationToken ct)
    {
        var entitlements = await service.GetResolvedEntitlementsAsync(id, ct);
        return TypedResults.Ok(entitlements.ToDictionary());
    }

    static async Task<Results<NoContent, NotFound<ProblemDetails>>> UpsertOverrideAsync(
        Guid id, string key, EntitlementOverrideRequest request,
        ISubscriptionService service, CancellationToken ct)
    {
        var result = await service.UpsertOverrideAsync(id, key, request.Value, request.Reason, ct);
        return result is null
            ? TypedResults.NotFound(PlanProblems.EntitlementNotFound(key))
            : TypedResults.NoContent();
    }

    static async Task<Results<NoContent, NotFound>> RemoveOverrideAsync(
        Guid id, string key, ISubscriptionService service, CancellationToken ct)
    {
        var removed = await service.RemoveOverrideAsync(id, key, ct);
        return removed ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static SubscriptionResponse ToResponse(TenantSubscription s) => new()
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
