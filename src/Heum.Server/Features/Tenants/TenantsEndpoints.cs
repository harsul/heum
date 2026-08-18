using System.Security.Claims;
using Heum.Infrastructure.Keycloak;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Server.Features.Tenants.Models;
using Heum.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Tenants;

public static class TenantsEndpoints
{
    public static RouteGroupBuilder MapTenantsEndpoints(this RouteGroupBuilder group)
    {
        var tenants = group.MapGroup("/tenants");

        tenants.MapPost("/register", RegisterTenantAsync)
            .WithName("RegisterTenant")
            .AllowAnonymous();

        // Self-service endpoints for a tenant's own admin(s) to manage their tenant, scoped to
        // whichever tenant the caller's token says they belong to (never a path parameter) -
        // gated by the "TenantAdmin" policy (the "Admin" realm role) so plain tenant users
        // can't reach this at all.
        var myTenant = tenants.MapGroup("/me").RequireAuthorization("TenantAdmin");

        myTenant.MapGet("/", GetMyTenantAsync)
            .WithName("GetMyTenant");

        myTenant.MapGet("/users", GetMyTenantUsersAsync)
            .WithName("GetMyTenantUsers");

        myTenant.MapPost("/users", AddMyTenantUserAsync)
            .WithName("AddMyTenantUser");

        myTenant.MapPost("/users/{userId}/enable", EnableMyTenantUserAsync)
            .WithName("EnableMyTenantUser");

        myTenant.MapPost("/users/{userId}/disable", DisableMyTenantUserAsync)
            .WithName("DisableMyTenantUser");

        return group;
    }

    private static async Task<Results<Created<RegisterTenantResponse>, Conflict<ProblemDetails>>> RegisterTenantAsync(
        CreateTenantRequest request,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var result = await tenantService.ProvisionTenantAsync(
            request.CompanyName,
            request.AdminEmail,
            cancellationToken);

        if (result.EmailConflict)
            return TypedResults.Conflict(TenantResponseMapper.EmailConflict(request.AdminEmail));

        return TypedResults.Created($"/api/tenants/{result.Tenant!.Id}", new RegisterTenantResponse
        {
            TenantId = result.Tenant.Id,
            Slug = result.Tenant.Slug,
            KeycloakUserId = result.KeycloakUserId!,
        });
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound, BadRequest<ProblemDetails>>> GetMyTenantAsync(
        ClaimsPrincipal user,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(user, out var tenantId))
            return NoTenantProblem();

        var tenant = await tenantService.GetTenantAsync(tenantId, cancellationToken);
        return tenant is null ? TypedResults.NotFound() : TypedResults.Ok(TenantResponseMapper.ToResponse(tenant));
    }

    internal static async Task<Results<Ok<IReadOnlyList<TenantUserResponse>>, BadRequest<ProblemDetails>>> GetMyTenantUsersAsync(
        ClaimsPrincipal user,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(user, out var tenantId))
            return NoTenantProblem();

        var users = await keycloakService.ListTenantUsersAsync(tenantId, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<TenantUserResponse>>(users.Select(TenantResponseMapper.ToResponse).ToList());
    }

    internal static async Task<Results<Created<TenantUserResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> AddMyTenantUserAsync(
        ClaimsPrincipal user,
        AddTenantUserRequest request,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(user, out var tenantId))
            return NoTenantProblem();

        var result = await tenantService.AddTenantUserAsync(tenantId, request.Email, cancellationToken);

        if (result.EmailConflict)
            return TypedResults.Conflict(TenantResponseMapper.EmailConflict(request.Email));

        return TypedResults.Created(
            $"/api/tenants/me/users/{result.KeycloakUserId}",
            TenantResponseMapper.NewlyCreatedUser(result.KeycloakUserId!, request.Email));
    }

    internal static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> EnableMyTenantUserAsync(
        ClaimsPrincipal user,
        string userId,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
        => await SetMyTenantUserEnabledAsync(user, userId, enabled: true, keycloakService, cancellationToken);

    internal static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> DisableMyTenantUserAsync(
        ClaimsPrincipal user,
        string userId,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
        => await SetMyTenantUserEnabledAsync(user, userId, enabled: false, keycloakService, cancellationToken);

    private static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> SetMyTenantUserEnabledAsync(
        ClaimsPrincipal user,
        string userId,
        bool enabled,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(user, out var tenantId))
            return NoTenantProblem();

        if (!enabled && string.Equals(userId, GetKeycloakUserId(user), StringComparison.Ordinal))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Cannot disable your own account",
                Detail = "You can't disable the account you're currently signed in with.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        var succeeded = await keycloakService.SetTenantUserEnabledAsync(tenantId, userId, enabled, cancellationToken);
        return succeeded ? TypedResults.NoContent() : TypedResults.NotFound();
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

    private static BadRequest<ProblemDetails> NoTenantProblem() => TypedResults.BadRequest(new ProblemDetails
    {
        Title = "No tenant associated with this account",
        Detail = "This account is not associated with a tenant.",
        Status = StatusCodes.Status400BadRequest,
    });
}
