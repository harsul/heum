using System.Net;
using System.Text.RegularExpressions;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Auditing;
using Heum.Data.Domain;
using Heum.Data.Models;
using Heum.Infrastructure.Keycloak.Services;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Services;

/// <inheritdoc cref="ITenantService" />
public sealed partial class TenantService(
    HeumDbContext dbContext,
    IKeycloakService keycloakService,
    IDomainEventCollector domainEventCollector,
    TimeProvider timeProvider) : ITenantService
{
    private const int MaxSlugSuffixAttempts = 50;

    public async Task<TenantProvisionResult> ProvisionTenantAsync(
        string companyName,
        string adminEmail,
        CancellationToken cancellationToken = default)
    {
        var slug = await GenerateUniqueSlugAsync(companyName, cancellationToken);

        var tenant = Tenant.Register(companyName, slug, timeProvider);
        var settings = TenantSettings.CreateDefault(tenant.Id, timeProvider);

        dbContext.Tenants.Add(tenant);
        dbContext.TenantSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var (keycloakUserId, emailConflict) = await CreateOnboardingUserAsync(
                tenant.Id, adminEmail, isTenantAdmin: true, cancellationToken);

            if (emailConflict)
            {
                dbContext.TenantSettings.Remove(settings);
                dbContext.Tenants.Remove(tenant);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new TenantProvisionResult(Tenant: null, KeycloakUserId: null, EmailConflict: true);
            }

            // Raises TenantCreatedEvent on the aggregate; this SaveChanges also flushes the
            // ambient UserOnboardingRequestedEvent queued by CreateOnboardingUserAsync above -
            // both are dispatched together, after commit, by DomainEventDispatchingInterceptor.
            tenant.MarkProvisioned(adminEmail, keycloakUserId!, timeProvider);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new TenantProvisionResult(tenant, keycloakUserId, EmailConflict: false);
        }
        catch
        {
            dbContext.TenantSettings.Remove(settings);
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

        // Adding a teammate to an existing tenant doesn't otherwise touch the DB, so this
        // SaveChanges exists purely to flush the ambient UserOnboardingRequestedEvent queued
        // by CreateOnboardingUserAsync above through DomainEventDispatchingInterceptor.
        if (!emailConflict)
            await dbContext.SaveChangesAsync(cancellationToken);

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

        tenant.Rename(name, timeProvider);
        tenant.SetActive(isActive, timeProvider);

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

        tenant.SetActive(isActive, timeProvider);

        await dbContext.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    public async Task<(IReadOnlyList<AuditTrail> Items, int TotalCount)> GetTenantHistoryAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Set<AuditTrail>()
            .Where(a => a.EntityName == nameof(Tenant) && a.PrimaryKey == tenantId.ToString());

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
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
                OccurredAt: timeProvider.GetUtcNow());

            // Not backed by any DB column change (Keycloak-only side effect), so it's queued
            // ambiently rather than raised on an aggregate - the next SaveChanges flushes it.
            domainEventCollector.Enqueue(onboardingRequested);

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
