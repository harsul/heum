using Heum.Server.Features.Tenants.Models;
using Refit;

namespace Heum.Server.xIntegration.Clients;

public interface IAdminTenantsApi
{
    [Get("/api/admin/tenants/")]
    Task<IApiResponse<List<TenantResponse>>> ListTenantsAsync(
        CancellationToken cancellationToken = default);

    [Post("/api/admin/tenants/")]
    Task<IApiResponse<TenantResponse>> CreateTenantAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/admin/tenants/{id}")]
    Task<IApiResponse<TenantResponse>> GetTenantAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/admin/tenants/{id}/users")]
    Task<IApiResponse<List<TenantUserResponse>>> GetTenantUsersAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/admin/tenants/{id}/history")]
    Task<IApiResponse<TenantHistoryResponse>> GetTenantHistoryAsync(
        Guid id,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    [Post("/api/admin/tenants/{id}/users")]
    Task<IApiResponse<TenantUserResponse>> AddTenantUserAsync(
        Guid id,
        AddTenantUserRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/admin/tenants/{id}/users/{userId}/enable")]
    Task<IApiResponse> EnableTenantUserAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    [Post("/api/admin/tenants/{id}/users/{userId}/disable")]
    Task<IApiResponse> DisableTenantUserAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    [Put("/api/admin/tenants/{id}")]
    Task<IApiResponse<TenantResponse>> UpdateTenantAsync(
        Guid id,
        UpdateTenantRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/admin/tenants/{id}/deactivate")]
    Task<IApiResponse<TenantResponse>> DeactivateTenantAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    [Post("/api/admin/tenants/{id}/reactivate")]
    Task<IApiResponse<TenantResponse>> ReactivateTenantAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
