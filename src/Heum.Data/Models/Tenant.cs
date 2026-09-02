using Heum.Contracts.Events;
using Heum.Data.Domain;

namespace Heum.Data.Models;

public class Tenant : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public string? LogoUrl { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Tenant()
    {
        // EF Core materialization.
    }

    public static Tenant Register(string name, string slug, TimeProvider timeProvider)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
        };

        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, tenant.Slug, timeProvider.GetUtcNow()));

        return tenant;
    }

    public void Rename(string name, TimeProvider timeProvider)
    {
        Name = name;
        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void SetActive(bool isActive, TimeProvider timeProvider)
    {
        IsActive = isActive;
        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void SetLogo(string? logoUrl, TimeProvider timeProvider)
    {
        LogoUrl = logoUrl;
        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }
}
