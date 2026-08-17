using System.Net;
using Azure.Messaging.ServiceBus;
using Heum.Data;
using Heum.Infrastructure.Keycloak;
using Heum.Infrastructure.Keycloak.Models;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Server.Features.Admin.Tenants.Models;
using Heum.Server.Features.Tenants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        HeumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenants = await dbContext.Tenants
            .OrderBy(t => t.Name)
            .Select(t => ToResponse(t))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<TenantResponse>>(tenants);
    }

    internal static async Task<Results<Created<TenantResponse>, Conflict<ProblemDetails>>> CreateTenantAsync(
        CreateTenantRequest request,
        HeumDbContext dbContext,
        IKeycloakService keycloakService,
        ServiceBusSender sender,
        CancellationToken cancellationToken)
    {
        var result = await TenantProvisioningService.ProvisionTenantAsync(
            request.CompanyName,
            request.Slug,
            request.AdminFirstName,
            request.AdminLastName,
            request.AdminEmail,
            request.AdminPassword,
            dbContext,
            keycloakService,
            sender,
            cancellationToken);

        if (result.SlugConflict)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Slug already in use",
                Detail = $"A tenant with slug '{request.Slug}' already exists.",
                Status = StatusCodes.Status409Conflict,
            });
        }

        var tenant = result.Tenant!;
        return TypedResults.Created($"/api/admin/tenants/{tenant.Id}", ToResponse(tenant));
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> GetTenantAsync(
        Guid id,
        HeumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.FindAsync([id], cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(ToResponse(tenant));
    }

    internal static async Task<Results<Ok<IReadOnlyList<TenantUserResponse>>, NotFound>> GetTenantUsersAsync(
        Guid id,
        HeumDbContext dbContext,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        var tenantExists = await dbContext.Tenants.AnyAsync(t => t.Id == id, cancellationToken);
        if (!tenantExists)
            return TypedResults.NotFound();

        var users = await keycloakService.ListTenantUsersAsync(id, cancellationToken);
        var response = users.Select(ToResponse).ToList();

        return TypedResults.Ok<IReadOnlyList<TenantUserResponse>>(response);
    }

    internal static async Task<Results<Created<TenantUserResponse>, NotFound, Conflict<ProblemDetails>>> AddTenantUserAsync(
        Guid id,
        AddTenantUserRequest request,
        HeumDbContext dbContext,
        IKeycloakService keycloakService,
        CancellationToken cancellationToken)
    {
        var tenantExists = await dbContext.Tenants.AnyAsync(t => t.Id == id, cancellationToken);
        if (!tenantExists)
            return TypedResults.NotFound();

        try
        {
            var keycloakUserId = await keycloakService.CreateTenantUserAsync(
                email: request.Email,
                firstName: request.FirstName,
                lastName: request.LastName,
                password: request.Password,
                tenantId: id,
                cancellationToken: cancellationToken);

            var response = new TenantUserResponse
            {
                Id = keycloakUserId,
                Username = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = true,
                EmailVerified = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            return TypedResults.Created($"/api/admin/tenants/{id}/users/{keycloakUserId}", response);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Email already in use",
                Detail = $"A user with email '{request.Email}' already exists.",
                Status = StatusCodes.Status409Conflict,
            });
        }
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> UpdateTenantAsync(
        Guid id,
        UpdateTenantRequest request,
        HeumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.FindAsync([id], cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        tenant.Name = request.Name;
        tenant.IsActive = request.IsActive;
        tenant.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ToResponse(tenant));
    }

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> DeactivateTenantAsync(
        Guid id,
        HeumDbContext dbContext,
        CancellationToken cancellationToken)
        => await SetActiveAsync(id, isActive: false, dbContext, cancellationToken);

    internal static async Task<Results<Ok<TenantResponse>, NotFound>> ReactivateTenantAsync(
        Guid id,
        HeumDbContext dbContext,
        CancellationToken cancellationToken)
        => await SetActiveAsync(id, isActive: true, dbContext, cancellationToken);

    private static async Task<Results<Ok<TenantResponse>, NotFound>> SetActiveAsync(
        Guid id,
        bool isActive,
        HeumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.FindAsync([id], cancellationToken);
        if (tenant is null)
            return TypedResults.NotFound();

        tenant.IsActive = isActive;
        tenant.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

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
