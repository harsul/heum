using Azure.Messaging.ServiceBus;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Models;
using Heum.Infrastructure.Keycloak;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Tenants;

/// <summary>
/// Shared tenant + Keycloak admin-user provisioning logic used both by self-service
/// registration (<see cref="TenantsEndpoints"/>) and by system-admin initiated tenant
/// creation (<see cref="Heum.Server.Features.Admin.Tenants.AdminTenantsEndpoints"/>).
/// </summary>
public static class TenantProvisioningService
{
    public sealed record ProvisionResult(Tenant? Tenant, string? KeycloakUserId, bool SlugConflict);

    public static async Task<ProvisionResult> ProvisionTenantAsync(
        string companyName,
        string slug,
        string adminFirstName,
        string adminLastName,
        string adminEmail,
        string adminPassword,
        HeumDbContext dbContext,
        IKeycloakAdminClient keycloakAdminClient,
        ServiceBusSender sender,
        CancellationToken cancellationToken)
    {
        var slugTaken = await dbContext.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken);
        if (slugTaken)
            return new ProvisionResult(Tenant: null, KeycloakUserId: null, SlugConflict: true);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = companyName,
            Slug = slug,
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var keycloakUserId = await keycloakAdminClient.ProvisionTenantAdminUserAsync(
                username: adminEmail,
                email: adminEmail,
                firstName: adminFirstName,
                lastName: adminLastName,
                password: adminPassword,
                tenantId: tenant.Id,
                cancellationToken: cancellationToken);

            var @event = new TenantCreatedEvent(
                TenantId: tenant.Id,
                Slug: tenant.Slug,
                AdminEmail: adminEmail,
                AdminFirstName: adminFirstName,
                AdminLastName: adminLastName,
                KeycloakUserId: keycloakUserId,
                OccurredAt: DateTimeOffset.UtcNow);

            await sender.SendMessageAsync(
                new ServiceBusMessage(BinaryData.FromObjectAsJson(@event))
                {
                    ContentType = "application/json",
                    Subject = nameof(TenantCreatedEvent),
                },
                cancellationToken);

            return new ProvisionResult(tenant, keycloakUserId, SlugConflict: false);
        }
        catch
        {
            // Provisioning the Keycloak user failed after the tenant record was committed;
            // roll back the tenant so provisioning can be safely retried.
            dbContext.Tenants.Remove(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
