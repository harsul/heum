using Heum.Server.Features.FeatureFlags.Models;
using Heum.Server.Features.FeatureFlags.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Heum.Server.Features.FeatureFlags.Endpoints;

public static class AdminFeaturesEndpoints
{
    public static RouteGroupBuilder MapAdminFeaturesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/features", GetAllAsync).WithName("AdminGetFeatureFlags");
        group.MapGet("/features/{name}", GetAsync).WithName("AdminGetFeatureFlag");
        group.MapPost("/features", CreateAsync).WithName("AdminCreateFeatureFlag");
        group.MapPut("/features/{name}", UpdateAsync).WithName("AdminUpdateFeatureFlag");
        group.MapDelete("/features/{name}", DeleteAsync).WithName("AdminDeleteFeatureFlag");
        return group;
    }

    // Returns all flags plus a flag indicating whether Azure App Config is wired up.
    internal static async Task<Ok<AdminFeatureFlagsResponse>> GetAllAsync(
        IFeatureFlagAdminService service,
        CancellationToken ct)
    {
        var flags = await service.GetAllAsync(ct);
        return TypedResults.Ok(new AdminFeatureFlagsResponse(service.IsManageable, flags));
    }

    internal static async Task<Results<Ok<FeatureFlagResponse>, NotFound>> GetAsync(
        string name,
        IFeatureFlagAdminService service,
        CancellationToken ct)
    {
        var flag = await service.GetAsync(name, ct);
        return flag is not null ? TypedResults.Ok(flag) : TypedResults.NotFound();
    }

    internal static async Task<Results<Created<FeatureFlagResponse>, BadRequest<string>, StatusCodeHttpResult>> CreateAsync(
        CreateFeatureFlagRequest request,
        IFeatureFlagAdminService service,
        CancellationToken ct)
    {
        if (!service.IsManageable)
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);

        if (string.IsNullOrWhiteSpace(request.Name))
            return TypedResults.BadRequest("Name is required.");

        var result = await service.CreateAsync(request, ct);
        return TypedResults.Created($"/api/admin/features/{result.Name}", result);
    }

    internal static async Task<Results<NoContent, NotFound, StatusCodeHttpResult>> UpdateAsync(
        string name,
        UpdateFeatureFlagRequest request,
        IFeatureFlagAdminService service,
        CancellationToken ct)
    {
        if (!service.IsManageable)
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var existing = await service.GetAsync(name, ct);
        if (existing is null) return TypedResults.NotFound();

        await service.UpdateAsync(name, request, ct);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, StatusCodeHttpResult>> DeleteAsync(
        string name,
        IFeatureFlagAdminService service,
        CancellationToken ct)
    {
        if (!service.IsManageable)
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var existing = await service.GetAsync(name, ct);
        if (existing is null) return TypedResults.NotFound();

        await service.DeleteAsync(name, ct);
        return TypedResults.NoContent();
    }
}

// Wraps the flag list with a boolean so the frontend knows whether management is possible.
public sealed record AdminFeatureFlagsResponse(bool IsManageable, IReadOnlyList<FeatureFlagResponse> Flags);
