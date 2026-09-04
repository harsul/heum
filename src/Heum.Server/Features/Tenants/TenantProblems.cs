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

    public static ProblemDetails InvalidContentType(string contentType) => new()
    {
        Title = "Unsupported content type",
        Detail = $"'{contentType}' is not allowed. Only image/jpeg and image/png are accepted.",
        Status = StatusCodes.Status400BadRequest,
    };

    public static ProblemDetails FileTooLarge() => new()
    {
        Title = "File too large",
        Detail = "The uploaded file exceeds the 2 MB limit.",
        Status = StatusCodes.Status400BadRequest,
    };

    public static ProblemDetails LogoUploadNotAllowed() => new()
    {
        Title = "Logo upload not included in plan",
        Detail = "Your current plan does not allow uploading a custom logo. Upgrade your plan to enable it.",
        Status = StatusCodes.Status403Forbidden,
    };

    public static ProblemDetails TenantDeactivated() => new()
    {
        Title = "Tenant deactivated",
        Detail = "This tenant has been deactivated. Contact support to restore access.",
        Status = StatusCodes.Status403Forbidden,
    };

    public static ProblemDetails TenantNotFound(Guid tenantId) => new()
    {
        Title = "Tenant not found",
        Detail = $"No tenant with id '{tenantId}' exists.",
        Status = StatusCodes.Status404NotFound,
    };
}
