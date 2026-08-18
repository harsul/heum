using Heum.Infrastructure.Messaging;

namespace Heum.Server.xUnit.Fakes;

/// <summary>Simple hand-written test double for <see cref="IEventPublisher"/> (no mocking library in this project).</summary>
public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly List<object> _publishedEvents = [];

    public IReadOnlyList<object> PublishedEvents => _publishedEvents;

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : notnull
    {
        _publishedEvents.Add(@event);
        return Task.CompletedTask;
    }
}
