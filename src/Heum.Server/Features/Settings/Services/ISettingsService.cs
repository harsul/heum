using Heum.Data.Models;

namespace Heum.Server.Features.Settings.Services;

public interface ISettingsService
{
    Task<TenantSettings> GetOrCreateAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantSettings> UpdateAsync(Guid tenantId, string locale, string timezone, CancellationToken cancellationToken = default);
}
