using Heum.Contracts.Events;
using Heum.Data.Domain;

namespace Heum.Data.Models;

public sealed class Plan : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public ICollection<PlanEntitlement> Entitlements { get; private set; } = [];

    private Plan() { }

    public static Plan Create(string name, TimeProvider timeProvider) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
    };

    public void Rename(string name, TimeProvider timeProvider)
    {
        Name = name;
        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void SetActive(bool active, TimeProvider timeProvider)
    {
        IsActive = active;
        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void UpsertEntitlement(Guid entitlementId, string value, TimeProvider timeProvider)
    {
        var existing = Entitlements.FirstOrDefault(e => e.EntitlementId == entitlementId);
        if (existing is not null)
            existing.SetValue(value);
        else
            Entitlements.Add(PlanEntitlement.Create(Id, entitlementId, value));

        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }
}
