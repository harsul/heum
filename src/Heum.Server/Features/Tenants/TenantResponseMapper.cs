using Heum.Data.Models;
using Heum.Infrastructure.Keycloak.Models;
using Heum.Server.Features.Admin.Tenants.Models;

namespace Heum.Server.Features.Tenants;

/// <summary>
/// Maps domain/Keycloak models to the response DTOs shared by <see cref="AdminTenantsEndpoints"/>
/// and the self-service "my tenant" endpoints in <see cref="TenantsEndpoints"/>, so both can
/// return an identically-shaped payload without duplicating the mapping logic.
/// </summary>
internal static class TenantResponseMapper
{
    public static TenantResponse ToResponse(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Slug = tenant.Slug,
        IsActive = tenant.IsActive,
        CreatedAtUtc = tenant.CreatedAtUtc,
        UpdatedAtUtc = tenant.UpdatedAtUtc,
    };

    public static TenantUserResponse ToResponse(KeycloakUserSummary user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Enabled = user.Enabled,
        EmailVerified = user.EmailVerified,
        CreatedAtUtc = user.CreatedTimestamp is { } timestamp
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
            : null,
    };
}
