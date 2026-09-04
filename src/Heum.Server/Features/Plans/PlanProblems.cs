using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Plans;

internal static class PlanProblems
{
    public static ProblemDetails PlanNameConflict(string name) => new()
    {
        Title = "Plan name already in use",
        Detail = $"A plan named '{name}' already exists.",
        Status = StatusCodes.Status409Conflict,
    };

    public static ProblemDetails EntitlementKeyConflict(string key) => new()
    {
        Title = "Entitlement key already in use",
        Detail = $"An entitlement with key '{key}' already exists.",
        Status = StatusCodes.Status409Conflict,
    };

    public static ProblemDetails PlanNotFound(Guid planId) => new()
    {
        Title = "Plan not found",
        Detail = $"No plan with id '{planId}' exists.",
        Status = StatusCodes.Status404NotFound,
    };

    public static ProblemDetails PlanInactive(Guid planId) => new()
    {
        Title = "Plan is inactive",
        Detail = $"Plan '{planId}' has been deactivated and can no longer be assigned to tenants.",
        Status = StatusCodes.Status400BadRequest,
    };

    public static ProblemDetails EntitlementNotFound(string key) => new()
    {
        Title = "Entitlement not found",
        Detail = $"No active entitlement with key '{key}' exists.",
        Status = StatusCodes.Status404NotFound,
    };
}
