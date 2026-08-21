namespace Heum.Infrastructure.Messaging;

/// <summary>
/// Single registry mapping event CLR types to Service Bus topics, and short type names back
/// to CLR types (for outbox deserialization). Adding a new event requires one
/// <see cref="MapTopic{TEvent}"/> call — no second registration needed.
/// </summary>
public sealed class EventTopicRegistry
{
    private readonly Dictionary<Type, string> _topicsByEventType = [];
    private readonly Dictionary<string, Type> _typesByName = [];

    public EventTopicRegistry MapTopic<TEvent>(string topicName) where TEvent : notnull
    {
        _topicsByEventType[typeof(TEvent)] = topicName;
        _typesByName[typeof(TEvent).Name] = typeof(TEvent);
        return this;
    }

    internal string GetTopic<TEvent>() =>
        _topicsByEventType.TryGetValue(typeof(TEvent), out var topic)
            ? topic
            : throw new InvalidOperationException(
                $"No Service Bus topic is registered for event type '{typeof(TEvent).Name}'. " +
                $"Call {nameof(MapTopic)}<{typeof(TEvent).Name}>(...) when configuring event publishing.");

    public Type ResolveType(string eventTypeName) =>
        _typesByName.TryGetValue(eventTypeName, out var type)
            ? type
            : throw new InvalidOperationException(
                $"No CLR type is registered for event type '{eventTypeName}'. " +
                $"Call {nameof(MapTopic)}<{eventTypeName}>(...) when configuring event publishing.");
}
