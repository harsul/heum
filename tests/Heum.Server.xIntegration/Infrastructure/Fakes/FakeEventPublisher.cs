using Heum.Infrastructure.Messaging;

namespace Heum.Server.xIntegration.Infrastructure.Fakes;

public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly List<object> _publishedEvents = [];

    public IReadOnlyList<object> PublishedEvents => _publishedEvents;

    public void Clear() => _publishedEvents.Clear();

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : notnull
    {
        _publishedEvents.Add(@event);
        return Task.CompletedTask;
    }
}
