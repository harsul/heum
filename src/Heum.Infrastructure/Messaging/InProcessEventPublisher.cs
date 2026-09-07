using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Heum.Infrastructure.Messaging;

/// <summary>
/// In-process event publisher backed by a <see cref="Channel{T}"/>. Intended for local
/// development and testing where a real Azure Service Bus is unavailable.
/// Select via configuration key <c>EventBus:Transport = "InProcess"</c>. Consumers can drain
/// <see cref="Reader"/> to process published events locally.
/// </summary>
internal sealed class InProcessEventPublisher(
    EventTopicRegistry topics,
    ILogger<InProcessEventPublisher> logger) : IEventPublisher
{
    private readonly Channel<(string Topic, BinaryData Payload)> _channel =
        Channel.CreateUnbounded<(string, BinaryData)>(
            new UnboundedChannelOptions { SingleWriter = false, AllowSynchronousContinuations = false });

    /// <summary>Allows a consumer or test to drain published events from the channel.</summary>
    public ChannelReader<(string Topic, BinaryData Payload)> Reader => _channel.Reader;

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default, string? messageId = null)
        where TEvent : notnull
    {
        var topic = topics.GetTopic<TEvent>();
        var payload = BinaryData.FromObjectAsJson(@event);
        logger.LogDebug("InProcess: publishing {EventType} to topic {Topic} (messageId={MessageId})",
            typeof(TEvent).Name, topic, messageId);
        _channel.Writer.TryWrite((topic, payload));
        return Task.CompletedTask;
    }
}
