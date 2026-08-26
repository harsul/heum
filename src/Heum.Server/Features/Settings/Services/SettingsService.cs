using Heum.Data;
using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.Features.Settings.Services;

internal sealed class SettingsService(HeumDbContext dbContext, TimeProvider timeProvider) : ISettingsService
{
    public async Task<TenantSettings> GetOrCreateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (settings is not null)
            return settings;

        settings = TenantSettings.CreateDefault(tenantId, timeProvider);
        dbContext.TenantSettings.Add(settings);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return settings;
        }
        catch (DbUpdateException)
        {
            // Another concurrent request inserted first (unique constraint on TenantId).
            // Detach the failed entity and return the row that now exists.
            dbContext.Entry(settings).State = EntityState.Detached;
            var existing = await dbContext.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
            if (existing is not null)
                return existing;
            throw;
        }
    }

    public async Task<TenantSettings> UpdateAsync(
        Guid tenantId,
        string locale,
        string timezone,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(tenantId, cancellationToken);
        settings.Update(locale, timezone, timeProvider);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
