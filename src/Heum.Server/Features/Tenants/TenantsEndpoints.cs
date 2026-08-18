using Heum.Server.Features.Tenants.Models;
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

        return group;
    }

    private static async Task<Results<Created<RegisterTenantResponse>, Conflict<ProblemDetails>>> RegisterTenantAsync(
        RegisterTenantRequest request,
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

        return TypedResults.Created($"/api/tenants/{result.Tenant!.Id}", new RegisterTenantResponse
        {
            TenantId = result.Tenant.Id,
            Slug = result.Tenant.Slug,
            KeycloakUserId = result.KeycloakUserId!,
        });
    }
}
