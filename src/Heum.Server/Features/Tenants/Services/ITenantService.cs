using Heum.Data.Models;
using Heum.Server.Features.Tenants.Endpoints;

namespace Heum.Server.Features.Tenants.Services;

public sealed record TenantUserProvisionResult(string? KeycloakUserId, bool EmailConflict, bool InvalidRole = false);

/// <summary>
/// Owns all tenant persistence logic (provisioning + CRUD) so that <see cref="TenantsEndpoints"/>
/// and <see cref="AdminTenantsEndpoints"/> can both depend on
/// it instead of talking to <c>HeumDbContext</c> directly.
/// </summary>
public interface ITenantService
{
    Task<Tenant> CreateTenantAsync(
        string companyName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds another user to an existing tenant, triggering the same onboarding email as
    /// tenant provisioning. Assumes the caller has already verified the tenant exists.
    /// </summary>
    /// <param name="role">
    /// An optional realm role to assign on top of the baseline "User" role (e.g. "Admin").
    /// Must be one of the roles returned by <see cref="IKeycloakService.GetAssignableRolesAsync"/>.
    /// Pass <c>null</c> to create a plain user with "User" only.
    /// </param>
    Task<TenantUserProvisionResult> AddTenantUserAsync(
        Guid tenantId,
        string email,
        string? role,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Tenant> Items, int TotalCount)> ListTenantsAsync(
        string? search,
        string? sortBy,
        string? sortDir,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

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

    Task<Tenant?> SetLogoAsync(Guid tenantId, string? logoUrl, CancellationToken cancellationToken = default);
}
