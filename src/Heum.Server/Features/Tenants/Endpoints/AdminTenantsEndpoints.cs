using Heum.Data.Models;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Server.Features.Settings.Models;
using Heum.Server.Features.Settings.Services;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.Features.Tenants.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Tenants.Endpoints;

public static class AdminTenantsEndpoints
{
    public static RouteGroupBuilder MapAdminTenantsEndpoints(this RouteGroupBuilder group)
    {
        var tenants = group.MapGroup("/tenants");

        tenants.MapGet("/", ListTenantsAsync)
            .WithName("AdminListTenants");

        tenants.MapPost("/", CreateTenantAsync)
            .WithName("AdminCreateTenant");

        tenants.MapGet("/{id:guid}", GetTenantAsync)
            .WithName("AdminGetTenant");

        tenants.MapGet("/{id:guid}/users", GetTenantUsersAsync)
            .WithName("AdminGetTenantUsers");

        tenants.MapGet("/{id:guid}/history", GetTenantHistoryAsync)
            .WithName("AdminGetTenantHistory");

        tenants.MapGet("/roles", GetAssignableRolesAsync)
            .WithName("AdminGetAssignableRoles");

        tenants.MapPost("/{id:guid}/users", AddTenantUserAsync)
            .WithName("AdminAddTenantUser");

        tenants.MapPost("/{id:guid}/users/{userId}/enable", EnableTenantUserAsync)
            .WithName("AdminEnableTenantUser");

        tenants.MapPost("/{id:guid}/users/{userId}/disable", DisableTenantUserAsync)
            .WithName("AdminDisableTenantUser");

        tenants.MapPut("/{id:guid}", UpdateTenantAsync)
            .WithName("AdminUpdateTenant");

        tenants.MapPost("/{id:guid}/deactivate", DeactivateTenantAsync)
            .WithName("AdminDeactivateTenant");

        tenants.MapPost("/{id:guid}/reactivate", ReactivateTenantAsync)
            .WithName("AdminReactivateTenant");

        tenants.MapGet("/{id:guid}/settings", GetTenantSettingsAsync)
            .WithName("AdminGetTenantSettings");

        tenants.MapPut("/{id:guid}/settings", UpdateTenantSettingsAsync)
            .WithName("AdminUpdateTenantSettings");

        return group;
    }

    internal static async Task<Ok<IReadOnlyList<TenantResponse>>> ListTenantsAsync(
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantService.ListTenantsAsync(cancellationToken);
        var response = tenants.Select(TenantResponseMapper.ToResponse).ToList();

        return TypedResults.Ok<IReadOnlyList<TenantResponse>>(response);
    }

    internal static async Task<Results<Created<TenantResponse>, Conflict<ProblemDetails>>> CreateTenantAsync(
        CreateTenantRequest request,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var result = await tenantService.ProvisionTenantAsync(
            request.CompanyName,
            request.AdminEmail,
            cancellationToken);

        if (result.EmailConflict)
            return TypedResults.Conflict(TenantProblems.EmailConflict(request.AdminEmail));

        var tenant = result.Tenant!;
        return TypedResults.Created($"/api/admin/tenants/{tenant.Id}", TenantResponseMapper.ToResponse(tenant));
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> GetTenantAsync(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(TenantResponseMapper.ToResponse(tenant));
    }

    internal static async Task<Results<Ok<IReadOnlyList<TenantUserResponse>>, NotFound>> GetTenantUsersAsync(
        Guid id,
        ITenantService tenantService,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        var users = await keycloakService.ListTenantUsersAsync(id, cancellationToken);
        var response = users.Select(TenantResponseMapper.ToResponse).ToList();

        return TypedResults.Ok<IReadOnlyList<TenantUserResponse>>(response);
    }

    internal static async Task<Results<Ok<TenantHistoryResponse>, NotFound>> GetTenantHistoryAsync(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var tenant = await tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        var (items, totalCount) = await tenantService.GetTenantHistoryAsync(id, page, pageSize, cancellationToken);

        return TypedResults.Ok(new TenantHistoryResponse
        {
            Items = items.Select(TenantResponseMapper.ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    internal static async Task<Ok<IReadOnlyList<string>>> GetAssignableRolesAsync(
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        var roles = await keycloakService.GetAssignableRolesAsync(cancellationToken);
        return TypedResults.Ok<IReadOnlyList<string>>(roles);
    }

    internal static async Task<Results<Created<TenantUserResponse>, NotFound, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> AddTenantUserAsync(
        Guid id,
        AddTenantUserRequest request,
        ITenantService tenantService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        var result = await tenantService.AddTenantUserAsync(id, request.Email, request.Role, cancellationToken);

        if (result.InvalidRole)
            return TypedResults.BadRequest(TenantProblems.InvalidRole(request.Role!));

        if (result.EmailConflict)
            return TypedResults.Conflict(TenantProblems.EmailConflict(request.Email));

        return TypedResults.Created(
            $"/api/admin/tenants/{id}/users/{result.KeycloakUserId}",
            TenantResponseMapper.NewlyCreatedUser(result.KeycloakUserId!, request.Email, timeProvider));
    }

    internal static async Task<Results<NoContent, NotFound>> EnableTenantUserAsync(
        Guid id,
        string userId,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
        => await SetTenantUserEnabledAsync(id, userId, enabled: true, keycloakService, cancellationToken);

    internal static async Task<Results<NoContent, NotFound>> DisableTenantUserAsync(
        Guid id,
        string userId,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
        => await SetTenantUserEnabledAsync(id, userId, enabled: false, keycloakService, cancellationToken);

    private static async Task<Results<NoContent, NotFound>> SetTenantUserEnabledAsync(
        Guid tenantId,
        string userId,
        bool enabled,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        var succeeded = await keycloakService.SetTenantUserEnabledAsync(tenantId, userId, enabled, cancellationToken);
        return succeeded ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> UpdateTenantAsync(
        Guid id,
        UpdateTenantRequest request,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.UpdateTenantAsync(id, request.Name, request.IsActive, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(TenantResponseMapper.ToResponse(tenant));
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> DeactivateTenantAsync(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken)
        => await SetActiveAsync(id, isActive: false, tenantService, cancellationToken);

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> ReactivateTenantAsync(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken)
        => await SetActiveAsync(id, isActive: true, tenantService, cancellationToken);

    internal static async Task<Results<Ok<TenantSettingsResponse>, NotFound>> GetTenantSettingsAsync(
        Guid id,
        ITenantService tenantService,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        var settings = await settingsService.GetOrCreateAsync(id, cancellationToken);
        return TypedResults.Ok(ToSettingsResponse(settings));
    }

    internal static async Task<Results<Ok<TenantSettingsResponse>, NotFound>> UpdateTenantSettingsAsync(
        Guid id,
        UpdateSettingsRequest request,
        ITenantService tenantService,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        var settings = await settingsService.UpdateAsync(id, request.Locale, request.Timezone, cancellationToken);
        return TypedResults.Ok(ToSettingsResponse(settings));
    }

    private static TenantSettingsResponse ToSettingsResponse(TenantSettings s) => new()
    {
        Locale = s.Locale,
        Timezone = s.Timezone,
        UpdatedAtUtc = s.UpdatedAtUtc,
    };

    private static async Task<Results<Ok<TenantResponse>, NotFound>> SetActiveAsync(
        Guid id,
        bool isActive,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.SetTenantActiveAsync(id, isActive, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(TenantResponseMapper.ToResponse(tenant));
    }
}
