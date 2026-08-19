using Heum.Server.Features.Tenants.Models;
using Refit;

namespace Heum.Server.xIntegration.Clients;

public interface IAdminTenantsApi
{
    [Get("/api/admin/tenants/")]
    Task<IApiResponse<List<TenantResponse>>> ListTenantsAsync(
        CancellationToken cancellationToken = default);

    [Get("/api/admin/tenants/{id}")]
    Task<IApiResponse<TenantResponse>> GetTenantAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
