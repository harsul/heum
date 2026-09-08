using Heum.Contracts.Events;
using Heum.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Heum.Server.xUnit;

public sealed class InProcessEventPublisherTests
{
    private readonly EventTopicRegistry _registry = new EventTopicRegistry()
        .MapTopic<TenantCreatedEvent>("tenant-events");

    private InProcessEventPublisher MakePublisher() =>
        new(_registry, NullLogger<InProcessEventPublisher>.Instance);

    [Fact]
    public async Task PublishAsync_WritesToChannel()
    {
        var publisher = MakePublisher();
        var evt = new TenantCreatedEvent(Guid.NewGuid(), "acme", DateTimeOffset.UtcNow);

        await publisher.PublishAsync(evt, TestContext.Current.CancellationToken);

        Assert.True(publisher.Reader.TryRead(out _));
    }

    [Fact]
    public async Task PublishAsync_WritesCorrectTopic()
    {
        var publisher = MakePublisher();
        var evt = new TenantCreatedEvent(Guid.NewGuid(), "acme", DateTimeOffset.UtcNow);

        await publisher.PublishAsync(evt, TestContext.Current.CancellationToken);

        publisher.Reader.TryRead(out var item);
        Assert.Equal("tenant-events", item.Topic);
    }

    [Fact]
    public async Task Reader_CanDrainMultiplePublishedEvents()
    {
        var publisher = MakePublisher();
        var evt = new TenantCreatedEvent(Guid.NewGuid(), "acme", DateTimeOffset.UtcNow);

        await publisher.PublishAsync(evt, TestContext.Current.CancellationToken);
        await publisher.PublishAsync(evt, TestContext.Current.CancellationToken);
        await publisher.PublishAsync(evt, TestContext.Current.CancellationToken);

        var count = 0;
        while (publisher.Reader.TryRead(out _)) count++;
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task PublishAsync_Throws_WhenTopicNotRegistered()
    {
        var publisher = new InProcessEventPublisher(
            new EventTopicRegistry(), // empty — no topics registered
            NullLogger<InProcessEventPublisher>.Instance);

        var evt = new TenantCreatedEvent(Guid.NewGuid(), "acme", DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(evt, TestContext.Current.CancellationToken));
    }
}
