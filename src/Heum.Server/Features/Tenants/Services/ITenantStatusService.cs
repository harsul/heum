namespace Heum.Server.Features.Tenants.Services;

/// <summary>
/// Answers "is this tenant currently allowed to use the API?" cheaply enough to be called on every
/// tenant-scoped request. Backed by the distributed cache with a short TTL; <see cref="TenantService"/>
/// invalidates the entry whenever a tenant is activated or deactivated so the change takes effect
/// immediately on every API instance.
/// </summary>
public interface ITenantStatusService
{
    /// <summary>Returns <c>true</c> when the tenant exists and is active.</summary>
    ValueTask<bool> IsActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
