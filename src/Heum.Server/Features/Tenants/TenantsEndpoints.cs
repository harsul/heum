using Azure.Messaging.ServiceBus;
using Heum.Contracts.Events;
using Heum.Server.Data;
using Heum.Server.Data.Models;
using Heum.Server.Features.Tenants.Models;
using Heum.Infrastructure.Keycloak;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        HeumdDbContext dbContext,
        IKeycloakAdminClient keycloakAdminClient,
        ServiceBusSender sender,
        CancellationToken cancellationToken)
    {
        var slugTaken = await dbContext.Tenants.AnyAsync(t => t.Slug == request.Slug, cancellationToken);
        if (slugTaken)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Slug already in use",
                Detail = $"A tenant with slug '{request.Slug}' already exists.",
                Status = StatusCodes.Status409Conflict,
            });
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName,
            Slug = request.Slug,
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var keycloakUserId = await keycloakAdminClient.ProvisionTenantAdminUserAsync(
                username: request.AdminEmail,
                email: request.AdminEmail,
                firstName: request.AdminFirstName,
                lastName: request.AdminLastName,
                password: request.AdminPassword,
                tenantId: tenant.Id,
                cancellationToken: cancellationToken);

            var @event = new TenantCreatedEvent(
                TenantId: tenant.Id,
                Slug: tenant.Slug,
                AdminEmail: request.AdminEmail,
                AdminFirstName: request.AdminFirstName,
                AdminLastName: request.AdminLastName,
                KeycloakUserId: keycloakUserId,
                OccurredAt: DateTimeOffset.UtcNow);

            await sender.SendMessageAsync(
                new ServiceBusMessage(BinaryData.FromObjectAsJson(@event))
                {
                    ContentType = "application/json",
                    Subject = nameof(TenantCreatedEvent),
                },
                cancellationToken);

            return TypedResults.Created($"/api/tenants/{tenant.Id}", new RegisterTenantResponse
            {
                TenantId = tenant.Id,
                Slug = tenant.Slug,
                KeycloakUserId = keycloakUserId,
            });
        }
        catch
        {
            // Provisioning the Keycloak user failed after the tenant record was committed;
            // roll back the tenant so registration can be safely retried.
            dbContext.Tenants.Remove(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
