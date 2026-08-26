using Heum.Data;
using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Settings.Services;

internal sealed class SettingsService(HeumDbContext dbContext, TimeProvider timeProvider) : ISettingsService
{
    public async Task<TenantSettings?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await dbContext.TenantSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

    public async Task<TenantSettings?> UpdateAsync(
        Guid tenantId,
        string locale,
        string timezone,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(tenantId, cancellationToken);
        if (settings is null)
            return null;

        settings.Update(locale, timezone, timeProvider);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
