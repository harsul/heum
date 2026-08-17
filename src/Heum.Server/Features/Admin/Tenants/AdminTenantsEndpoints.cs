using Heum.Infrastructure.Keycloak.Models;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Server.Features.Admin.Tenants.Models;
using Heum.Server.Features.Tenants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Admin.Tenants;

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

        tenants.MapPost("/{id:guid}/users", AddTenantUserAsync)
            .WithName("AdminAddTenantUser");

        tenants.MapPut("/{id:guid}", UpdateTenantAsync)
            .WithName("AdminUpdateTenant");

        tenants.MapPost("/{id:guid}/deactivate", DeactivateTenantAsync)
            .WithName("AdminDeactivateTenant");

        tenants.MapPost("/{id:guid}/reactivate", ReactivateTenantAsync)
            .WithName("AdminReactivateTenant");

        return group;
    }

    internal static async Task<Ok<IReadOnlyList<TenantResponse>>> ListTenantsAsync(
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantService.ListTenantsAsync(cancellationToken);
        var response = tenants.Select(ToResponse).ToList();

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
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Email already in use",
                Detail = $"A user with email '{request.AdminEmail}' already exists.",
                Status = StatusCodes.Status409Conflict,
            });
        }

        var tenant = result.Tenant!;
        return TypedResults.Created($"/api/admin/tenants/{tenant.Id}", ToResponse(tenant));
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> GetTenantAsync(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(ToResponse(tenant));
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
        var response = users.Select(ToResponse).ToList();

        return TypedResults.Ok<IReadOnlyList<TenantUserResponse>>(response);
    }

    internal static async Task<Results<Created<TenantUserResponse>, NotFound, Conflict<ProblemDetails>>> AddTenantUserAsync(
        Guid id,
        AddTenantUserRequest request,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetTenantAsync(id, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        var result = await tenantService.AddTenantUserAsync(id, request.Email, cancellationToken);

        if (result.EmailConflict)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Email already in use",
                Detail = $"A user with email '{request.Email}' already exists.",
                Status = StatusCodes.Status409Conflict,
            });
        }

        var response = new TenantUserResponse
        {
            Id = result.KeycloakUserId!,
            Username = request.Email,
            Email = request.Email,
            FirstName = null,
            LastName = null,
            Enabled = true,
            EmailVerified = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        return TypedResults.Created($"/api/admin/tenants/{id}/users/{result.KeycloakUserId}", response);
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

        return TypedResults.Ok(ToResponse(tenant));
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

    private static async Task<Results<Ok<TenantResponse>, NotFound>> SetActiveAsync(
        Guid id,
        bool isActive,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.SetTenantActiveAsync(id, isActive, cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(ToResponse(tenant));
    }

    private static TenantResponse ToResponse(Data.Models.Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Slug = tenant.Slug,
        IsActive = tenant.IsActive,
        CreatedAtUtc = tenant.CreatedAtUtc,
        UpdatedAtUtc = tenant.UpdatedAtUtc,
    };

    private static TenantUserResponse ToResponse(KeycloakUserSummary user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Enabled = user.Enabled,
        EmailVerified = user.EmailVerified,
        CreatedAtUtc = user.CreatedTimestamp is { } timestamp
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
            : null,
    };
}
