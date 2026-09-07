using Heum.Contracts.Events;
using Heum.Infrastructure.Messaging;

namespace Heum.Server.xUnit;

/// <summary>
/// Guards against the failure mode where a service enqueues a domain event that the outbox
/// processor has no topic for: such a message retries until it is permanently abandoned.
/// </summary>
public sealed class EventTopicMapTests
{
    [Fact]
    public void MapDomainEvents_CoversEveryDomainEventInContracts()
    {
        var registry = new EventTopicRegistry().MapDomainEvents();

        var eventTypes = typeof(IDomainEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(eventTypes);

        var unmapped = eventTypes
            .Where(t =>
            {
                try
                {
                    registry.ResolveType(t.Name);
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            })
            .Select(t => t.Name)
            .ToList();

        Assert.True(unmapped.Count == 0,
            $"Domain events without a topic in {nameof(EventTopicMap)}.{nameof(EventTopicMap.MapDomainEvents)}: {string.Join(", ", unmapped)}");
    }
}
