using Heum.Data.Models;
using Heum.Server.Features.Plans.Models;
using Heum.Server.Features.Plans.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Plans.Endpoints;

public static class AdminEntitlementsEndpoints
{
    public static RouteGroupBuilder MapAdminEntitlementsEndpoints(this RouteGroupBuilder group)
    {
        var entitlements = group.MapGroup("/entitlements");
        entitlements.MapGet("/", ListEntitlementsAsync).WithName("ListEntitlements");
        entitlements.MapPost("/", CreateEntitlementAsync).WithName("CreateEntitlement");
        return group;
    }

    static async Task<Ok<List<EntitlementResponse>>> ListEntitlementsAsync(
        IPlanAdminService service, CancellationToken ct)
    {
        var items = await service.ListEntitlementsAsync(ct);
        return TypedResults.Ok(items.Select(ToResponse).ToList());
    }

    static async Task<Results<Created<EntitlementResponse>, Conflict<ProblemDetails>>> CreateEntitlementAsync(
        CreateEntitlementRequest request, IPlanAdminService service, CancellationToken ct)
    {
        var entitlement = await service.CreateEntitlementAsync(request.Key, request.Type, request.Description, ct);
        if (entitlement is null)
            return TypedResults.Conflict(PlanProblems.EntitlementKeyConflict(request.Key));

        return TypedResults.Created($"/api/admin/entitlements/{entitlement.Id}", ToResponse(entitlement));
    }

    private static EntitlementResponse ToResponse(Entitlement e) => new()
    {
        Id = e.Id,
        Key = e.Key,
        Type = e.Type.ToString(),
        Description = e.Description,
        IsActive = e.IsActive,
    };
}
