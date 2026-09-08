using Heum.Server.Features.FeatureFlags.Models;

namespace Heum.Server.Features.FeatureFlags.Services;

/// <summary>
/// Read-only admin service used when Azure App Configuration is not configured.
/// Reads feature flags from the FeatureManagement section in appsettings.json.
/// Mutating operations throw <see cref="InvalidOperationException"/>.
/// </summary>
internal sealed class LocalFeatureFlagAdminService : IFeatureFlagAdminService
{
    private readonly IConfiguration _configuration;

    public LocalFeatureFlagAdminService(IConfiguration configuration) => _configuration = configuration;

    public bool IsManageable => false;

    public Task<IReadOnlyList<FeatureFlagResponse>> GetAllAsync(CancellationToken ct)
    {
        var section = _configuration.GetSection("FeatureManagement");
        var flags = section.GetChildren()
            .Select(child =>
            {
                // Simple scalar: "FlagName": true/false
                // Object shape: "FlagName": { ... } — just show as disabled locally
                var isEnabled = child.Value != null && bool.TryParse(child.Value, out var b) && b;
                return new FeatureFlagResponse(child.Key, null, isEnabled, [], [], 0);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<FeatureFlagResponse>>(flags);
    }

    public Task<FeatureFlagResponse?> GetAsync(string name, CancellationToken ct)
    {
        var value = _configuration[$"FeatureManagement:{name}"];
        if (value is null) return Task.FromResult<FeatureFlagResponse?>(null);
        var isEnabled = bool.TryParse(value, out var b) && b;
        return Task.FromResult<FeatureFlagResponse?>(new FeatureFlagResponse(name, null, isEnabled, [], [], 0));
    }

    public Task<FeatureFlagResponse> CreateAsync(CreateFeatureFlagRequest request, CancellationToken ct) =>
        Task.FromException<FeatureFlagResponse>(NotConfigured());

    public Task UpdateAsync(string name, UpdateFeatureFlagRequest request, CancellationToken ct) =>
        Task.FromException(NotConfigured());

    public Task DeleteAsync(string name, CancellationToken ct) =>
        Task.FromException(NotConfigured());

    private static InvalidOperationException NotConfigured() =>
        new("Azure App Configuration is not configured. Set ConnectionStrings:appconfig to enable flag management.");
}
