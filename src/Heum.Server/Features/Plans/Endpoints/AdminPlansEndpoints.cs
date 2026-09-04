using Heum.Data.Models;
using Heum.Server.Features.Plans.Models;
using Heum.Server.Features.Plans.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Plans.Endpoints;

public static class AdminPlansEndpoints
{
    public static RouteGroupBuilder MapAdminPlansEndpoints(this RouteGroupBuilder group)
    {
        var plans = group.MapGroup("/plans");

        plans.MapGet("/", ListPlansAsync).WithName("ListPlans");
        plans.MapPost("/", CreatePlanAsync).WithName("CreatePlan");
        plans.MapGet("/{id:guid}", GetPlanAsync).WithName("GetPlan");
        plans.MapPut("/{id:guid}", UpdatePlanAsync).WithName("UpdatePlan");
        plans.MapPut("/{id:guid}/entitlements/{key}", UpsertPlanEntitlementAsync).WithName("UpsertPlanEntitlement");

        return group;
    }

    static async Task<Ok<List<PlanResponse>>> ListPlansAsync(
        IPlanAdminService service, CancellationToken ct)
    {
        var plans = await service.ListPlansAsync(ct);
        return TypedResults.Ok(plans.Select(ToResponse).ToList());
    }

    static async Task<Results<Created<PlanResponse>, Conflict<ProblemDetails>>> CreatePlanAsync(
        CreatePlanRequest request, IPlanAdminService service, CancellationToken ct)
    {
        var plan = await service.CreatePlanAsync(request.Name, ct);
        if (plan is null)
            return TypedResults.Conflict(PlanProblems.PlanNameConflict(request.Name));

        return TypedResults.Created($"/api/admin/plans/{plan.Id}", ToResponse(plan));
    }

    static async Task<Results<Ok<PlanResponse>, NotFound>> GetPlanAsync(
        Guid id, IPlanAdminService service, CancellationToken ct)
    {
        var plan = await service.GetPlanAsync(id, ct);
        return plan is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(plan));
    }

    static async Task<Results<Ok<PlanResponse>, NotFound>> UpdatePlanAsync(
        Guid id, UpdatePlanRequest request, IPlanAdminService service, CancellationToken ct)
    {
        var plan = await service.UpdatePlanAsync(id, request.Name, request.IsActive, ct);
        return plan is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(plan));
    }

    static async Task<Results<NoContent, NotFound>> UpsertPlanEntitlementAsync(
        Guid id, string key, UpsertPlanEntitlementRequest request, IPlanAdminService service, CancellationToken ct)
    {
        var ok = await service.UpsertPlanEntitlementAsync(id, key, request.Value, ct);
        return ok ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static PlanResponse ToResponse(Plan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        IsActive = plan.IsActive,
        CreatedAtUtc = plan.CreatedAtUtc,
        UpdatedAtUtc = plan.UpdatedAtUtc,
        Entitlements = plan.Entitlements.Select(pe => new PlanEntitlementResponse
        {
            Key = pe.Entitlement.Key,
            Type = pe.Entitlement.Type.ToString(),
            Value = pe.Value,
        }).ToList(),
    };
}
