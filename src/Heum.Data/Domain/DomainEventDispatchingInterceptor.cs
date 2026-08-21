using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heum.Data.Domain;

/// <summary>
/// Before a <c>SaveChanges</c> commits, collects domain events raised on tracked aggregate roots
/// (see <see cref="AggregateRoot"/>) plus any ambient events queued via
/// <see cref="IDomainEventCollector"/>, and writes each one as an <see cref="OutboxMessage"/> row
/// tracked on the same <see cref="DbContext"/> - so it's persisted atomically with whatever
/// entity change raised it (the "transactional outbox" pattern). A separate poller
/// (<c>Heum.Server</c>'s outbox processor) reads and publishes these rows later; this interceptor
/// never talks to Service Bus itself, so <c>Heum.Data</c> has no messaging dependency.
/// </summary>
public class DomainEventDispatchingInterceptor(IDomainEventCollector collector, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        WriteOutboxMessages(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        WriteOutboxMessages(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteOutboxMessages(DbContext? context)
    {
        if (context is null)
            return;

        var aggregatesWithEvents = context.ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(e => e.DomainEvents)
            .Concat(collector.DequeueAll())
            .ToList();

        foreach (var aggregate in aggregatesWithEvents)
            aggregate.ClearDomainEvents();

        if (domainEvents.Count == 0)
            return;

        foreach (var domainEvent in domainEvents)
        {
            context.Set<OutboxMessage>().Add(new OutboxMessage
            {
                EventType = domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            });
        }
    }
}
