using Heum.Contracts.Events;

namespace Heum.Server.Services;

/// <summary>
/// Maps the short CLR type names stored in <see cref="Heum.Data.Domain.OutboxMessage.EventType"/>
/// back to their concrete <see cref="IDomainEvent"/> type, so <see cref="OutboxProcessor"/> can
/// deserialize each row's JSON payload. Mirrors <see cref="Heum.Infrastructure.Messaging.EventTopicRegistry"/>'s
/// style: adding a new domain event type requires one more <see cref="Register{TEvent}"/> call here.
/// </summary>
public sealed class OutboxEventTypeCatalog
{
    private readonly Dictionary<string, Type> _typesByName = [];

    public OutboxEventTypeCatalog Register<TEvent>() where TEvent : IDomainEvent
    {
        _typesByName[typeof(TEvent).Name] = typeof(TEvent);
        return this;
    }

    internal Type Resolve(string eventType) =>
        _typesByName.TryGetValue(eventType, out var type)
            ? type
            : throw new InvalidOperationException(
                $"No CLR type is registered for outbox event type '{eventType}'. " +
                $"Call {nameof(Register)}<{eventType}>(...) when configuring the outbox catalog.");
}
