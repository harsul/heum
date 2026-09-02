namespace Heum.Data.Models;

public enum EntitlementType { Boolean, Integer, Decimal }

public sealed class Entitlement
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public EntitlementType Type { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Entitlement() { }

    public static Entitlement Create(string key, EntitlementType type, string? description = null)
        => new() { Id = Guid.NewGuid(), Key = key, Type = type, Description = description };

    public void SetActive(bool active) => IsActive = active;
    public void SetDescription(string? description) => Description = description;
}
