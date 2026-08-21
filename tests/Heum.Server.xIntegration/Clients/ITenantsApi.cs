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

    [Get("/api/tenants/me/users")]
    Task<IApiResponse<List<TenantUserResponse>>> GetMyTenantUsersAsync(
        CancellationToken cancellationToken = default);

    [Post("/api/tenants/me/users")]
    Task<IApiResponse<TenantUserResponse>> AddMyTenantUserAsync(
        AddTenantUserRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/tenants/me/users/{userId}/enable")]
    Task<IApiResponse> EnableMyTenantUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    [Post("/api/tenants/me/users/{userId}/disable")]
    Task<IApiResponse> DisableMyTenantUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    [Get("/api/tenants/me/history")]
    Task<IApiResponse<TenantHistoryResponse>> GetMyTenantHistoryAsync(
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}