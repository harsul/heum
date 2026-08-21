namespace Heum.Server.Services;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool HasTenant { get; }
}
