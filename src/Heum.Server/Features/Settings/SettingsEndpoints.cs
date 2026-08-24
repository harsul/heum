using Heum.Data.Models;
using Heum.Server.Features.Settings.Models;
using Heum.Server.Features.Settings.Services;
using Heum.Server.Features.Tenants;
using Heum.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Settings;

public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder group)
    {
        var settings = group.MapGroup("/settings").RequireAuthorization("TenantAdmin");

        settings.MapGet("/", GetSettingsAsync)
            .WithName("GetSettings");

        settings.MapPut("/", UpdateSettingsAsync)
            .WithName("UpdateSettings");

        return group;
    }

    internal static async Task<Results<Ok<TenantSettingsResponse>, BadRequest<ProblemDetails>>> GetSettingsAsync(
        ITenantContext tenantContext,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var settings = await settingsService.GetOrCreateAsync(tenantContext.TenantId, cancellationToken);
        return TypedResults.Ok(ToResponse(settings));
    }

    internal static async Task<Results<Ok<TenantSettingsResponse>, BadRequest<ProblemDetails>>> UpdateSettingsAsync(
        ITenantContext tenantContext,
        UpdateSettingsRequest request,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var settings = await settingsService.UpdateAsync(
            tenantContext.TenantId,
            request.Locale,
            request.Timezone,
            cancellationToken);

        return TypedResults.Ok(ToResponse(settings));
    }

    private static TenantSettingsResponse ToResponse(TenantSettings settings) => new()
    {
        Locale = settings.Locale,
        Timezone = settings.Timezone,
        UpdatedAtUtc = settings.UpdatedAtUtc,
    };
}
