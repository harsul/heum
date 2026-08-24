using Heum.Infrastructure.Messaging;

namespace Heum.Server.xUnit.Fakes;

/// <summary>Simple hand-written test double for <see cref="IEventPublisher"/> (no mocking library in this project).</summary>
public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly List<object> _publishedEvents = [];

    public IReadOnlyList<object> PublishedEvents => _publishedEvents;

    /// <summary>When set, every <see cref="PublishAsync{TEvent}"/> call throws this instead of succeeding.</summary>
    public Exception? ExceptionToThrow { get; set; }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default, string? messageId = null) where TEvent : notnull
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        _publishedEvents.Add(@event);
        return Task.CompletedTask;
    }
}
