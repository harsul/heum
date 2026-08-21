using Heum.Data.Auditing;
using Heum.Data.Models;
using Heum.Server.Features.Tenants;

namespace Heum.Server.Services;

public sealed record TenantProvisionResult(Tenant? Tenant, string? KeycloakUserId, bool EmailConflict);

public sealed record TenantUserProvisionResult(string? KeycloakUserId, bool EmailConflict);

/// <summary>
/// Owns all tenant persistence logic (provisioning + CRUD) so that <see cref="TenantsEndpoints"/>
/// and <see cref="Heum.Server.Features.Admin.Tenants.AdminTenantsEndpoints"/> can both depend on
/// it instead of talking to <c>HeumDbContext</c> directly.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Registers a new tenant (generating a unique slug from the company name) and provisions
    /// its first (admin) user in Keycloak. Rolls back the tenant record if Keycloak provisioning
    /// fails (e.g. the email is already in use), so registration can be safely retried.
    /// </summary>
    Task<TenantProvisionResult> ProvisionTenantAsync(
        string companyName,
        string adminEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds another user to an existing tenant, triggering the same onboarding email as
    /// tenant provisioning. Assumes the caller has already verified the tenant exists.
    /// </summary>
    Task<TenantUserProvisionResult> AddTenantUserAsync(
        Guid tenantId,
        string email,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken cancellationToken = default);

    Task<Tenant?> GetTenantAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Tenant?> UpdateTenantAsync(
        Guid id,
        string name,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<Tenant?> SetTenantActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of <see cref="AuditTrail"/> entries recorded against the given tenant's
    /// <see cref="Tenant"/> row (name/active-state changes etc.), newest first.
    /// </summary>
    Task<(IReadOnlyList<AuditTrail> Items, int TotalCount)> GetTenantHistoryAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
