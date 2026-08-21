namespace Heum.Data.Multitenancy;

public interface ITenantProvider
{
    Guid? TenantId { get; }
}
