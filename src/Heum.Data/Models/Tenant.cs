using Heum.Contracts.Events;
using Heum.Data.Domain;

namespace Heum.Data.Models;

public class Tenant : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }

    private Tenant()
    {
        // EF Core materialization.
    }

    public static Tenant Register(string name, string slug) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = slug,
    };

    /// <summary>
    /// Raises <see cref="TenantCreatedEvent"/> once the tenant's first admin has been
    /// successfully provisioned in Keycloak (called from <c>TenantService.ProvisionTenantAsync</c>).
    /// </summary>
    public void MarkProvisioned(string adminEmail, string keycloakUserId) =>
        AddDomainEvent(new TenantCreatedEvent(Id, Slug, adminEmail, keycloakUserId, DateTimeOffset.UtcNow));

    public void Rename(string name)
    {
        Name = name;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
