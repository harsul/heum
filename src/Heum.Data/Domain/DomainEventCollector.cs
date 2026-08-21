using Heum.Contracts.Events;

namespace Heum.Data.Domain;

/// <inheritdoc cref="IDomainEventCollector" />
public sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    public void Enqueue(IDomainEvent domainEvent) => _pendingEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> DequeueAll()
    {
        if (_pendingEvents.Count == 0)
            return [];

        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();
        return events;
    }
}
