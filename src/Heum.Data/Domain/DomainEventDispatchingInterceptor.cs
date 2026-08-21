using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heum.Data.Domain;

/// <summary>
/// After a successful <c>SaveChanges</c>, collects domain events raised on tracked aggregate
/// roots (see <see cref="AggregateRoot"/>) plus any ambient events queued via
/// <see cref="IDomainEventCollector"/>, and hands them to <see cref="IDomainEventDispatcher"/> -
/// so services never need to publish events themselves. Dispatches post-commit (not in
/// <c>SavingChanges</c>) so nothing is published for a write that could still fail.
/// </summary>
public class DomainEventDispatchingInterceptor(
    IDomainEventDispatcher dispatcher,
    IDomainEventCollector collector) : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEventsAsync(eventData.Context).GetAwaiter().GetResult();

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken = default)
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

        await dispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}
