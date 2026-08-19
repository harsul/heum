using Heum.Server.Features.Tenants.Models;
using Refit;

namespace Heum.Server.xIntegration.Clients;

public interface ITenantsApi
{
    [Post("/api/tenants/register")]
    Task<IApiResponse<RegisterTenantResponse>> RegisterTenantAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/tenants/me/")]
    Task<IApiResponse<TenantResponse>> GetMyTenantAsync(
        CancellationToken cancellationToken = default);
}