using Heum.Contracts.Events;

namespace Heum.Data.Domain;

/// <summary>
/// Base class for entities whose state changes should raise domain events (e.g.
/// <c>Tenant.MarkProvisioned</c> raising <c>TenantCreatedEvent</c>), instead of callers building
/// and publishing those events by hand.
/// </summary>
public abstract class AggregateRoot : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
