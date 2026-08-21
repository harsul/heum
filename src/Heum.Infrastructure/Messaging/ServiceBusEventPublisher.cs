using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Heum.Infrastructure.Messaging;

/// <summary>
/// Publishes events to Azure Service Bus, resolving the target topic for each event type via
/// <see cref="EventTopicRegistry"/> and lazily creating/caching one <see cref="ServiceBusSender"/>
/// per topic for the lifetime of the app.
/// </summary>
internal sealed class ServiceBusEventPublisher(ServiceBusClient client, EventTopicRegistry topics) : IEventPublisher
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default, string? messageId = null) where TEvent : notnull
    {
        var topic = topics.GetTopic<TEvent>();
        var sender = _senders.GetOrAdd(topic, client.CreateSender);

        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(@event))
        {
            ContentType = "application/json",
            Subject = typeof(TEvent).Name,
        };

        if (messageId is not null)
            message.MessageId = messageId;

        return sender.SendMessageAsync(message, cancellationToken);
    }
}
