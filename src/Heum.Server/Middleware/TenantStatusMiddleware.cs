using Heum.Infrastructure.Keycloak;
using Heum.Server.Features.Tenants;
using Heum.Server.Features.Tenants.Services;

namespace Heum.Server.Middleware;

/// <summary>
/// Rejects every request carrying a <c>tenant_id</c> claim whose tenant has been deactivated (or
/// no longer exists) with 403. Requests without a tenant claim - anonymous callers, SystemAdmins,
/// service accounts - pass straight through. Sits after authorization so the endpoint's own
/// policy still produces 401/403 first for unauthenticated or under-privileged callers.
/// </summary>
internal sealed class TenantStatusMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantStatusService tenantStatusService)
    {
        var claim = context.User.FindFirst(KeycloakClaimTypes.TenantId)?.Value;

        if (claim is null || !Guid.TryParse(claim, out var tenantId))
        {
            await next(context);
            return;
        }

        if (!await tenantStatusService.IsActiveAsync(tenantId, context.RequestAborted))
        {
            var problem = TenantProblems.TenantDeactivated();
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
