using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Tenants;

internal static class TenantProblems
{
    public static ProblemDetails NoTenant() => new()
    {
        Title = "No tenant associated with this account",
        Detail = "This account is not associated with a tenant.",
        Status = StatusCodes.Status400BadRequest,
    };

    public static ProblemDetails EmailConflict(string email) => new()
    {
        Title = "Email already in use",
        Detail = $"A user with email '{email}' already exists.",
        Status = StatusCodes.Status409Conflict,
    };

    public static ProblemDetails CannotDisableSelf() => new()
    {
        Title = "Cannot disable your own account",
        Detail = "You can't disable the account you're currently signed in with.",
        Status = StatusCodes.Status400BadRequest,
    };

    public static ProblemDetails InvalidRole(string role) => new()
    {
        Title = "Invalid role",
        Detail = $"'{role}' is not an assignable role. Use GET /api/tenants/me/roles to retrieve valid options.",
        Status = StatusCodes.Status400BadRequest,
    };
}
