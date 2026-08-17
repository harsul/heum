namespace Heum.Infrastructure.Messaging;

/// <summary>
/// Publishes domain/integration events without callers needing to know which transport
/// (Service Bus today, potentially something else later) or topic is used underneath.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : notnull;
}
