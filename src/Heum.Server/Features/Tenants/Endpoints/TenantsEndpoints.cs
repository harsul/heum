using System.Security.Claims;
using Heum.Infrastructure.Keycloak;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Server.Common;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.Features.Tenants.Services;
using Heum.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Tenants.Endpoints;

public static class TenantsEndpoints
{
    public static RouteGroupBuilder MapTenantsEndpoints(this RouteGroupBuilder group)
    {
        var tenants = group.MapGroup("/tenants");

        // Self-service endpoints for a tenant's own admin(s) to manage their tenant, scoped to
        // whichever tenant the caller's token says they belong to (never a path parameter) -
        // gated by the "TenantAdmin" policy (the "Admin" realm role) so plain tenant users
        // can't reach this at all.
        var myTenant = tenants.MapGroup("/me").RequireAuthorization("TenantAdmin");

        myTenant.MapGet("/", GetMyTenantAsync)
            .WithName("GetMyTenant");

        myTenant.MapGet("/users", GetMyTenantUsersAsync)
            .WithName("GetMyTenantUsers");

        myTenant.MapGet("/roles", GetMyTenantAssignableRolesAsync)
            .WithName("GetMyTenantAssignableRoles");

        myTenant.MapPost("/users", AddMyTenantUserAsync)
            .WithName("AddMyTenantUser");

        myTenant.MapPost("/users/{userId}/enable", EnableMyTenantUserAsync)
            .WithName("EnableMyTenantUser");

        myTenant.MapPost("/users/{userId}/disable", DisableMyTenantUserAsync)
            .WithName("DisableMyTenantUser");

        myTenant.MapGet("/history", GetMyTenantHistoryAsync)
            .WithName("GetMyTenantHistory");

        return group;
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound, BadRequest<ProblemDetails>>> GetMyTenantAsync(
        ITenantContext tenantContext,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var tenant = await tenantService.GetTenantAsync(tenantContext.TenantId, cancellationToken);
        return tenant is null ? TypedResults.NotFound() : TypedResults.Ok(TenantResponseMapper.ToResponse(tenant));
    }

    internal static async Task<Results<Ok<IReadOnlyList<TenantUserResponse>>, BadRequest<ProblemDetails>>> GetMyTenantUsersAsync(
        ITenantContext tenantContext,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var users = await keycloakService.ListTenantUsersAsync(tenantContext.TenantId, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<TenantUserResponse>>(users.Select(TenantResponseMapper.ToResponse).ToList());
    }

    internal static async Task<Ok<IReadOnlyList<string>>> GetMyTenantAssignableRolesAsync(
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        var roles = await keycloakService.GetAssignableRolesAsync(cancellationToken);
        return TypedResults.Ok<IReadOnlyList<string>>(roles);
    }

    internal static async Task<Results<Created<TenantUserResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> AddMyTenantUserAsync(
        ITenantContext tenantContext,
        AddTenantUserRequest request,
        ITenantService tenantService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var result = await tenantService.AddTenantUserAsync(tenantContext.TenantId, request.Email, request.Role, cancellationToken);

        if (result.InvalidRole)
            return TypedResults.BadRequest(TenantProblems.InvalidRole(request.Role!));

        if (result.EmailConflict)
            return TypedResults.Conflict(TenantProblems.EmailConflict(request.Email));

        return TypedResults.Created(
            $"/api/tenants/me/users/{result.KeycloakUserId}",
            TenantResponseMapper.NewlyCreatedUser(result.KeycloakUserId!, request.Email, timeProvider));
    }

    internal static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> EnableMyTenantUserAsync(
        ClaimsPrincipal user,
        string userId,
        ITenantContext tenantContext,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
        => await SetMyTenantUserEnabledAsync(user, userId, enabled: true, tenantContext, keycloakService, cancellationToken);

    internal static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> DisableMyTenantUserAsync(
        ClaimsPrincipal user,
        string userId,
        ITenantContext tenantContext,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
        => await SetMyTenantUserEnabledAsync(user, userId, enabled: false, tenantContext, keycloakService, cancellationToken);

    private static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> SetMyTenantUserEnabledAsync(
        ClaimsPrincipal user,
        string userId,
        bool enabled,
        ITenantContext tenantContext,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        if (!enabled && string.Equals(userId, GetKeycloakUserId(user), StringComparison.Ordinal))
            return TypedResults.BadRequest(TenantProblems.CannotDisableSelf());

        var succeeded = await keycloakService.SetTenantUserEnabledAsync(tenantContext.TenantId, userId, enabled, cancellationToken);
        return succeeded ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    internal static async Task<Results<Ok<PagedResponse<TenantHistoryEntryResponse>>, BadRequest<ProblemDetails>>> GetMyTenantHistoryAsync(
        ITenantContext tenantContext,
        ITenantService tenantService,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var (items, totalCount) = await tenantService.GetTenantHistoryAsync(tenantContext.TenantId, page, pageSize, cancellationToken);

        return TypedResults.Ok(new PagedResponse<TenantHistoryEntryResponse>
        {
            Items = items.Select(TenantResponseMapper.ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    private static string? GetKeycloakUserId(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;

    /// <summary>
    /// Reads the <see cref="KeycloakClaimTypes.TenantId"/> claim (populated by a Keycloak protocol
    /// mapper off the user's "tenant_id" attribute) off the caller's token. Accounts without a
    /// tenant (e.g. SystemAdmin/service accounts) won't have this claim.
    /// </summary>
    internal static bool TryGetTenantId(ClaimsPrincipal user, out Guid tenantId)
    {
        var claim = user.FindFirst(KeycloakClaimTypes.TenantId)?.Value;
        return Guid.TryParse(claim, out tenantId);
    }

}
