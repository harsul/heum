using Heum.Server.Features.Plans.Models;
using Heum.Server.Features.Subscriptions.Models;
using Refit;

namespace Heum.Server.xIntegration.Clients;

public interface IAdminPlansApi
{
    [Get("/api/admin/plans/")]
    Task<IApiResponse<List<PlanResponse>>> ListPlansAsync(CancellationToken cancellationToken = default);

    [Post("/api/admin/plans/")]
    Task<IApiResponse<PlanResponse>> CreatePlanAsync(
        CreatePlanRequest request,
        CancellationToken cancellationToken = default);

    [Put("/api/admin/plans/{id}")]
    Task<IApiResponse<PlanResponse>> UpdatePlanAsync(
        Guid id,
        UpdatePlanRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/admin/entitlements/")]
    Task<IApiResponse<EntitlementResponse>> CreateEntitlementAsync(
        CreateEntitlementRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/admin/{tenantId}/subscription")]
    Task<IApiResponse<SubscriptionResponse>> GetCurrentSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    [Post("/api/admin/{tenantId}/subscription")]
    Task<IApiResponse<SubscriptionResponse>> AssignPlanAsync(
        Guid tenantId,
        AssignPlanRequest request,
        CancellationToken cancellationToken = default);

    [Put("/api/admin/{tenantId}/entitlements/{key}")]
    Task<IApiResponse> UpsertOverrideAsync(
        Guid tenantId,
        string key,
        EntitlementOverrideRequest request,
        CancellationToken cancellationToken = default);
}
