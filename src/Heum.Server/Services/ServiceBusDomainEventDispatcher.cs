using System.Collections.Concurrent;
using System.Reflection;
using Heum.Contracts.Events;
using Heum.Data.Domain;
using Heum.Infrastructure.Messaging;

namespace Heum.Server.Services;

/// <summary>
/// Publishes domain events collected by <see cref="DomainEventDispatchingInterceptor"/> to
/// Service Bus via <see cref="IEventPublisher"/>. Resolves <see cref="IEventPublisher.PublishAsync{TEvent}"/>
/// for each event's concrete runtime type via cached reflection, so adding a new domain event
/// type only requires registering it with <see cref="EventTopicRegistry"/> - nothing here changes.
/// </summary>
internal sealed class ServiceBusDomainEventDispatcher(IEventPublisher eventPublisher) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> PublishMethods = new();
    private static readonly MethodInfo PublishAsyncDefinition =
        typeof(IEventPublisher).GetMethod(nameof(IEventPublisher.PublishAsync))!;

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var publishMethod = PublishMethods.GetOrAdd(
                domainEvent.GetType(),
                eventType => PublishAsyncDefinition.MakeGenericMethod(eventType));

            await (Task)publishMethod.Invoke(eventPublisher, [domainEvent, cancellationToken])!;
        }
    }
}
