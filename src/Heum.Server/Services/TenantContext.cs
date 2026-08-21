using Heum.Data.Multitenancy;
using Heum.Infrastructure.Keycloak;

namespace Heum.Server.Services;

public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext, ITenantProvider
{
    private Guid? _tenantId;
    private bool _resolved;

    public bool HasTenant
    {
        get
        {
            Resolve();
            return _tenantId.HasValue;
        }
    }

    public Guid TenantId
    {
        get
        {
            Resolve();
            return _tenantId ?? throw new InvalidOperationException(
                "No tenant is associated with the current request.");
        }
    }

    Guid? ITenantProvider.TenantId
    {
        get
        {
            Resolve();
            return _tenantId;
        }
    }

    private void Resolve()
    {
        if (_resolved)
            return;

        _resolved = true;
        var claim = httpContextAccessor.HttpContext?.User.FindFirst(KeycloakClaimTypes.TenantId)?.Value;
        if (Guid.TryParse(claim, out var id))
            _tenantId = id;
    }
}
