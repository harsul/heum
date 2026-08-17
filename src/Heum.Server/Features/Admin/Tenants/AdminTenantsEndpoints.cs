using Azure.Messaging.ServiceBus;
using Heum.Data;
using Heum.Infrastructure.Keycloak;
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
        IKeycloakAdminClient keycloakAdminClient,
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
            keycloakAdminClient,
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
}
