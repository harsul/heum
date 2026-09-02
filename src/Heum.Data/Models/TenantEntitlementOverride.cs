using Heum.Data.Multitenancy;

namespace Heum.Data.Models;

/// <summary>
/// Per-tenant entitlement value that takes precedence over the plan default.
/// Implements <see cref="ITenantEntity"/> so EF's global query filter scopes reads to the current tenant.
/// </summary>
public sealed class TenantEntitlementOverride : ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid EntitlementId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Entitlement Entitlement { get; private set; } = null!;

    private TenantEntitlementOverride() { }

    public static TenantEntitlementOverride Create(
        Guid tenantId,
        Guid entitlementId,
        string value,
        string? reason,
        TimeProvider timeProvider) => new()
    {
        TenantId = tenantId,
        EntitlementId = entitlementId,
        Value = value,
        Reason = reason,
        CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
    };

    public void SetValue(string value, string? reason) { Value = value; Reason = reason; }
}
