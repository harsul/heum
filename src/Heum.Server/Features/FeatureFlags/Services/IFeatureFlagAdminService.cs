using Heum.Server.Features.FeatureFlags.Models;

namespace Heum.Server.Features.FeatureFlags.Services;

internal interface IFeatureFlagAdminService
{
    /// <summary>
    /// True when backed by Azure App Configuration (full CRUD).
    /// False when reading from appsettings.json only (no mutations).
    /// </summary>
    bool IsManageable { get; }

    Task<IReadOnlyList<FeatureFlagResponse>> GetAllAsync(CancellationToken ct);
    Task<FeatureFlagResponse?> GetAsync(string name, CancellationToken ct);
    Task<FeatureFlagResponse> CreateAsync(CreateFeatureFlagRequest request, CancellationToken ct);
    Task UpdateAsync(string name, UpdateFeatureFlagRequest request, CancellationToken ct);
    Task DeleteAsync(string name, CancellationToken ct);
}
