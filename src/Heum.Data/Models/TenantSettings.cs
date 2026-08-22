using Heum.Data.Multitenancy;

namespace Heum.Data.Models;

public class TenantSettings : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Locale { get; private set; } = "en";
    public string Timezone { get; private set; } = "UTC";
    public DateTime UpdatedAtUtc { get; private set; }

    private TenantSettings() { }

    public static TenantSettings CreateDefault(Guid tenantId, TimeProvider timeProvider) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
    };

    public void Update(string locale, string timezone, TimeProvider timeProvider)
    {
        Locale = locale;
        Timezone = timezone;
        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }
}
