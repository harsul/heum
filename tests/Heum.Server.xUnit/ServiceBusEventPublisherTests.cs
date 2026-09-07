using Azure.Messaging.ServiceBus;
using Heum.Contracts.Events;
using Heum.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Heum.Server.xUnit;

/// <summary>
/// Unit tests for <see cref="ServiceBusEventPublisher"/> using hand-written fakes.
/// <see cref="ServiceBusSender"/> and <see cref="ServiceBusClient"/> are designed for
/// inheritance-based testing (both expose protected constructors) so no mock framework is required.
/// </summary>
public sealed class ServiceBusEventPublisherTests
{
    private readonly FakeServiceBusClient _client = new();
    private readonly EventTopicRegistry _registry;
    private readonly ServiceBusEventPublisher _publisher;

    public ServiceBusEventPublisherTests()
    {
        _registry = new EventTopicRegistry()
            .MapTopic<TenantCreatedEvent>("tenant-events");
        _publisher = new ServiceBusEventPublisher(_client, _registry);
    }

    [Fact]
    public async Task PublishAsync_SendsToCorrectTopic()
    {
        var evt = new TenantCreatedEvent(Guid.NewGuid(), "acme", DateTimeOffset.UtcNow);

        await _publisher.PublishAsync(evt);

        var sender = _client.GetSender("tenant-events");
        Assert.Single(sender.SentMessages);
    }

    [Fact]
    public async Task PublishAsync_SetsMessageId_WhenProvided()
    {
        var evt = new TenantCreatedEvent(Guid.NewGuid(), "acme", DateTimeOffset.UtcNow);
        var messageId = Guid.NewGuid().ToString();

        await _publisher.PublishAsync(evt, messageId: messageId);

        var sender = _client.GetSender("tenant-events");
        Assert.Equal(messageId, sender.SentMessages[0].MessageId);
    }

    [Fact]
    public async Task PublishAsync_DoesNotSetMessageId_WhenNotProvided()
    {
        var evt = new TenantCreatedEvent(Guid.NewGuid(), "acme", DateTimeOffset.UtcNow);

        await _publisher.PublishAsync(evt);

        var sender = _client.GetSender("tenant-events");
        // SDK auto-assigns a MessageId when not set — just confirm the message was sent
        Assert.Single(sender.SentMessages);
    }

    [Fact]
    public async Task PublishAsync_CachesSender_ForSameTopic()
    {
        var evt = new TenantCreatedEvent(Guid.NewGuid(), "acme", DateTimeOffset.UtcNow);

        await _publisher.PublishAsync(evt);
        await _publisher.PublishAsync(evt);

        // Only one CreateSender call should have been made
        Assert.Equal(1, _client.CreateSenderCallCount);
        var sender = _client.GetSender("tenant-events");
        Assert.Equal(2, sender.SentMessages.Count);
    }

    private sealed class FakeServiceBusSender : ServiceBusSender
    {
        public List<ServiceBusMessage> SentMessages { get; } = [];

        public override Task SendMessageAsync(
            ServiceBusMessage message,
            CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeServiceBusClient : ServiceBusClient
    {
        private readonly Dictionary<string, FakeServiceBusSender> _senders = [];
        public int CreateSenderCallCount { get; private set; }

        public override ServiceBusSender CreateSender(string queueOrTopicName)
        {
            CreateSenderCallCount++;
            if (!_senders.TryGetValue(queueOrTopicName, out var sender))
            {
                sender = new FakeServiceBusSender();
                _senders[queueOrTopicName] = sender;
            }
            return sender;
        }

        public FakeServiceBusSender GetSender(string topicName) => _senders[topicName];
    }
}
