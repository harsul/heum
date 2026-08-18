using System.Net;
using System.Text.RegularExpressions;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Models;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Services;

/// <inheritdoc cref="ITenantService" />
public sealed partial class TenantService(
    HeumDbContext dbContext,
    IKeycloakService keycloakService,
    IEventPublisher eventPublisher) : ITenantService
{
    private const int MaxSlugSuffixAttempts = 50;

    public async Task<TenantProvisionResult> ProvisionTenantAsync(
        string companyName,
        string adminEmail,
        CancellationToken cancellationToken = default)
    {
        var slug = await GenerateUniqueSlugAsync(companyName, cancellationToken);

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
            var (keycloakUserId, emailConflict) = await CreateOnboardingUserAsync(
                tenant.Id, adminEmail, isTenantAdmin: true, cancellationToken);

            if (emailConflict)
            {
                dbContext.Tenants.Remove(tenant);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new TenantProvisionResult(Tenant: null, KeycloakUserId: null, EmailConflict: true);
            }

            var tenantCreated = new TenantCreatedEvent(
                TenantId: tenant.Id,
                Slug: tenant.Slug,
                AdminEmail: adminEmail,
                KeycloakUserId: keycloakUserId!,
                OccurredAt: DateTimeOffset.UtcNow);

            await eventPublisher.PublishAsync(tenantCreated, cancellationToken);

            return new TenantProvisionResult(tenant, keycloakUserId, EmailConflict: false);
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

    public async Task<TenantUserProvisionResult> AddTenantUserAsync(
        Guid tenantId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var (keycloakUserId, emailConflict) = await CreateOnboardingUserAsync(
            tenantId, email, isTenantAdmin: false, cancellationToken);
        return new TenantUserProvisionResult(keycloakUserId, emailConflict);
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

    /// <summary>
    /// Creates the Keycloak user for a tenant (first admin or additional teammate - identical
    /// either way) and publishes <see cref="UserOnboardingRequestedEvent"/> on success. Shared
    /// by <see cref="ProvisionTenantAsync"/> and <see cref="AddTenantUserAsync"/>.
    /// </summary>
    private async Task<(string? KeycloakUserId, bool EmailConflict)> CreateOnboardingUserAsync(
        Guid tenantId,
        string email,
        bool isTenantAdmin,
        CancellationToken cancellationToken)
    {
        try
        {
            var keycloakUserId = await keycloakService.CreateTenantUserAsync(
                email, tenantId, isTenantAdmin, cancellationToken);

            var onboardingRequested = new UserOnboardingRequestedEvent(
                TenantId: tenantId,
                Email: email,
                KeycloakUserId: keycloakUserId,
                OccurredAt: DateTimeOffset.UtcNow);

            await eventPublisher.PublishAsync(onboardingRequested, cancellationToken);

            return (keycloakUserId, EmailConflict: false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return (null, EmailConflict: true);
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(string companyName, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(companyName);

        for (var attempt = 1; attempt <= MaxSlugSuffixAttempts; attempt++)
        {
            var candidate = attempt == 1 ? baseSlug : $"{baseSlug}-{attempt}";
            if (!await dbContext.Tenants.AnyAsync(t => t.Slug == candidate, cancellationToken))
                return candidate;
        }

        // Truncate so the slug + "-" + 8-char hex suffix never exceeds the 100-char DB column.
        var truncated = baseSlug.Length > 91 ? baseSlug[..91] : baseSlug;
        return $"{truncated}-{Guid.NewGuid():N}"[..100];
    }

    private static string Slugify(string value)
    {
        var slug = NonAlphanumericRunRegex().Replace(value.ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "tenant" : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRunRegex();
}
