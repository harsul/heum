using Heum.Infrastructure.Messaging;

namespace Heum.Server.xIntegration.Infrastructure.Fakes;

public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly List<object> _publishedEvents = [];

    public IReadOnlyList<object> PublishedEvents => _publishedEvents;

    /// <summary>When set, every <see cref="PublishAsync{TEvent}"/> call throws this instead of succeeding.</summary>
    public Exception? ExceptionToThrow { get; set; }

    public void Clear() => _publishedEvents.Clear();

    public void Reset()
    {
        _publishedEvents.Clear();
        ExceptionToThrow = null;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : notnull
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        _publishedEvents.Add(@event);
        return Task.CompletedTask;
    }
}
