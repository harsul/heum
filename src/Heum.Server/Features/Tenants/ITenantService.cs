using Heum.Data.Models;

namespace Heum.Server.Features.Tenants;

public sealed record TenantProvisionResult(Tenant? Tenant, string? KeycloakUserId, bool SlugConflict);

/// <summary>
/// Owns all tenant persistence logic (provisioning + CRUD) so that <see cref="TenantsEndpoints"/>
/// and <see cref="Heum.Server.Features.Admin.Tenants.AdminTenantsEndpoints"/> can both depend on
/// it instead of talking to <c>HeumDbContext</c> directly.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Registers a new tenant and provisions its first (admin) user in Keycloak. Rolls back
    /// the tenant record if Keycloak provisioning fails, so registration can be safely retried.
    /// </summary>
    Task<TenantProvisionResult> ProvisionTenantAsync(
        string companyName,
        string slug,
        string adminFirstName,
        string adminLastName,
        string adminEmail,
        string adminPassword,
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
}
