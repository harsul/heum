using Heum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Heum.Server.Features.Tenants.Services;

/// <inheritdoc cref="ITenantStatusService" />
internal sealed class TenantStatusService(
    HeumDbContext dbContext,
    IDistributedCache cache,
    ILogger<TenantStatusService> logger) : ITenantStatusService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly byte[] ActiveMarker = [1];
    private static readonly byte[] InactiveMarker = [0];

    private static string CacheKey(Guid tenantId) => $"tenant:status:{tenantId}";

    public async ValueTask<bool> IsActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(tenantId);

        try
        {
            var cached = await cache.GetAsync(key, cancellationToken);
            if (cached is { Length: 1 })
                return cached[0] == 1;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Distributed cache unavailable when reading status for tenant {TenantId} — falling back to DB", tenantId);
        }

        // Tenant is not an ITenantEntity, so no global filter interferes with this lookup.
        var isActive = await dbContext.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        try
        {
            await cache.SetAsync(
                key,
                isActive ? ActiveMarker : InactiveMarker,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Distributed cache unavailable when caching status for tenant {TenantId}", tenantId);
        }

        return isActive;
    }

    public async Task InvalidateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(CacheKey(tenantId), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Distributed cache unavailable when invalidating status for tenant {TenantId}", tenantId);
        }
    }
}
