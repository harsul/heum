using Heum.Data.Multitenancy;

namespace Heum.Server.xUnit.Fakes;

public sealed class FakeTenantProvider(Guid? tenantId = null) : ITenantProvider
{
    public Guid? TenantId { get; set; } = tenantId;
}
