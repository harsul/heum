using Azure.Messaging.ServiceBus;
using Heum.Data;
using Heum.Infrastructure.Keycloak;
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

        return TypedResults.Created($"/api/tenants/{result.Tenant!.Id}", new RegisterTenantResponse
        {
            TenantId = result.Tenant.Id,
            Slug = result.Tenant.Slug,
            KeycloakUserId = result.KeycloakUserId!,
        });
    }
}
