using Heum.Server.Features.Settings.Models;
using Refit;

namespace Heum.Server.xIntegration.Clients;

public interface ISettingsApi
{
    [Get("/api/settings")]
    Task<IApiResponse<TenantSettingsResponse>> GetSettingsAsync(
        CancellationToken cancellationToken = default);

    [Put("/api/settings")]
    Task<IApiResponse<TenantSettingsResponse>> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default);
}
