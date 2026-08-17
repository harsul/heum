namespace Heum.Infrastructure.Messaging;

/// <summary>
/// Maps event CLR types to the Service Bus topic they should be published to. Configured
/// once at startup via <see cref="MessagingExtensions.AddEventPublishing{TBuilder}"/> so
/// that adding a new event type only requires one more <see cref="MapTopic{TEvent}"/> call,
/// without touching <see cref="IEventPublisher"/> or any of its consumers.
/// </summary>
public sealed class EventTopicRegistry
{
    private readonly Dictionary<Type, string> _topicsByEventType = [];

    public EventTopicRegistry MapTopic<TEvent>(string topicName) where TEvent : notnull
    {
        _topicsByEventType[typeof(TEvent)] = topicName;
        return this;
    }

    internal string GetTopic<TEvent>() =>
        _topicsByEventType.TryGetValue(typeof(TEvent), out var topic)
            ? topic
            : throw new InvalidOperationException(
                $"No Service Bus topic is registered for event type '{typeof(TEvent).Name}'. " +
                $"Call {nameof(MapTopic)}<{typeof(TEvent).Name}>(...) when configuring event publishing.");
}
