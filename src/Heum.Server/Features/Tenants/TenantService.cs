using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Models;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Tenants;

/// <inheritdoc cref="ITenantService" />
public sealed class TenantService(
    HeumDbContext dbContext,
    IKeycloakService keycloakService,
    IEventPublisher eventPublisher) : ITenantService
{
    public async Task<TenantProvisionResult> ProvisionTenantAsync(
        string companyName,
        string slug,
        string adminFirstName,
        string adminLastName,
        string adminEmail,
        string adminPassword,
        CancellationToken cancellationToken = default)
    {
        var slugTaken = await dbContext.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken);
        if (slugTaken)
            return new TenantProvisionResult(Tenant: null, KeycloakUserId: null, SlugConflict: true);

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
            var keycloakUserId = await keycloakService.ProvisionTenantAdminUserAsync(
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

            await eventPublisher.PublishAsync(@event, cancellationToken);

            return new TenantProvisionResult(tenant, keycloakUserId, SlugConflict: false);
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

    public async Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Tenants
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task<Tenant?> GetTenantAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Tenants.FindAsync([id], cancellationToken);

    public async Task<Tenant?> UpdateTenantAsync(
        Guid id,
        string name,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants.FindAsync([id], cancellationToken);
        if (tenant is null)
            return null;

        tenant.Name = name;
        tenant.IsActive = isActive;
        tenant.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    public async Task<Tenant?> SetTenantActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants.FindAsync([id], cancellationToken);
        if (tenant is null)
            return null;

        tenant.IsActive = isActive;
        tenant.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return tenant;
    }
}
