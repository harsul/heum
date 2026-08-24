namespace Heum.Server.Features.Tenants;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool HasTenant { get; }
}
