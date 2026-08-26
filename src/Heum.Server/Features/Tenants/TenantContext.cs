using Heum.Data.Multitenancy;
using Heum.Infrastructure.Keycloak;

namespace Heum.Server.Features.Tenants;

public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext, ITenantProvider
{
    private readonly Lazy<Guid?> _tenantId = new(() =>
    {
        var claim = httpContextAccessor.HttpContext?.User.FindFirst(KeycloakClaimTypes.TenantId)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    });

    public bool HasTenant => _tenantId.Value.HasValue;

    public Guid TenantId => _tenantId.Value ?? throw new InvalidOperationException(
        "No tenant is associated with the current request.");

    Guid? ITenantProvider.TenantId => _tenantId.Value;
}