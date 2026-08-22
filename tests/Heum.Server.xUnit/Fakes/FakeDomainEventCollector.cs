using Heum.Contracts.Events;
using Heum.Data.Domain;

namespace Heum.Server.xUnit.Fakes;

public sealed class FakeDomainEventCollector : IDomainEventCollector
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyList<IDomainEvent> EnqueuedEvents => _events;

    public void Enqueue(IDomainEvent domainEvent) => _events.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> DequeueAll()
    {
        var snapshot = _events.ToList();
        _events.Clear();
        return snapshot;
    }
}
