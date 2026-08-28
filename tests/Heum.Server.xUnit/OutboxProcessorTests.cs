using System.Text.Json;
using Heum.BackgroundService.Outbox;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Infrastructure.Messaging;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Heum.Server.xUnit;

/// <summary>
/// Exercises <see cref="OutboxProcessor"/> directly against an in-memory <see cref="HeumDbContext"/>,
/// independent of the ASP.NET host - see the integration suite for "an API call produces outbox
/// rows" coverage.
/// </summary>
public sealed class OutboxProcessorTests : IDisposable
{
    private const int DefaultMaxAttempts = 5;

    private readonly HeumDbContext _db;
    private readonly FakeEventPublisher _events = new();
    private readonly EventTopicRegistry _registry = new EventTopicRegistry().MapTopic<TenantCreatedEvent>("tenant-events");
    private readonly OutboxProcessorOptions _options = new() { MaxAttempts = DefaultMaxAttempts };
    private readonly OutboxProcessor _processor;

    public OutboxProcessorTests()
    {
        var dbOptions = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HeumDbContext(dbOptions);
        _processor = new OutboxProcessor(
            _db,
            _events,
            _registry,
            Options.Create(_options),
            TimeProvider.System,
            NullLogger<OutboxProcessor>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ProcessPendingAsync_PublishesAndMarksProcessed_ForAPendingMessage()
    {
        var tenantId = Guid.NewGuid();
        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            tenantId, "acme", DateTimeOffset.UtcNow));

        await _processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var message = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(message.ProcessedAtUtc);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.LastError);

        var published = Assert.Single(_events.PublishedEvents);
        var publishedEvent = Assert.IsType<TenantCreatedEvent>(published);
        Assert.Equal(tenantId, publishedEvent.TenantId);
    }

    [Fact]
    public async Task ProcessPendingAsync_StopsRetrying_OnceMaxAttemptsIsReached()
    {
        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "acme", DateTimeOffset.UtcNow));

        _events.ExceptionToThrow = new InvalidOperationException("Service Bus is unreachable");

        for (var attempt = 1; attempt <= DefaultMaxAttempts; attempt++)
        {
            await _processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

            var message = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(attempt, message.Attempts);
            Assert.Null(message.ProcessedAtUtc);
            Assert.Contains("Service Bus is unreachable", message.LastError);
        }

        // One more cycle: the row now has Attempts == MaxAttempts, so it's excluded from the
        // query entirely - Attempts must stay exactly at the cap even though publishing would
        // still fail if it were retried.
        await _processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var finalMessage = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DefaultMaxAttempts, finalMessage.Attempts);
        Assert.Null(finalMessage.ProcessedAtUtc);

        // Even if the downstream issue is later fixed, a message that's exhausted its attempts
        // is left alone rather than silently retried forever.
        _events.ExceptionToThrow = null;
        await _processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var afterFixMessage = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DefaultMaxAttempts, afterFixMessage.Attempts);
        Assert.Null(afterFixMessage.ProcessedAtUtc);
        Assert.Empty(_events.PublishedEvents);
    }

    [Fact]
    public async Task ProcessPendingAsync_DoesNotReprocess_AlreadyProcessedMessages()
    {
        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "acme", DateTimeOffset.UtcNow));

        await _processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
        await _processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        Assert.Single(_events.PublishedEvents);
    }

    [Fact]
    public async Task ProcessPendingAsync_RespectsBatchSize()
    {
        _options.BatchSize = 1;
        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "acme", DateTimeOffset.UtcNow));
        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "beta", DateTimeOffset.UtcNow));

        await _processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        Assert.Single(_events.PublishedEvents);
        Assert.Equal(1, await _db.OutboxMessages.CountAsync(m => m.ProcessedAtUtc != null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProcessPendingAsync_RemovesProcessedMessages_OlderThanRetentionPeriod()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var processor = new OutboxProcessor(
            _db,
            _events,
            _registry,
            Options.Create(new OutboxProcessorOptions { MaxAttempts = DefaultMaxAttempts, RetentionPeriod = TimeSpan.FromDays(1) }),
            fakeTime,
            NullLogger<OutboxProcessor>.Instance);

        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "acme", DateTimeOffset.UtcNow));

        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, await _db.OutboxMessages.CountAsync(TestContext.Current.CancellationToken));

        fakeTime.Advance(TimeSpan.FromDays(2));
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, await _db.OutboxMessages.CountAsync(TestContext.Current.CancellationToken));
    }

    private async Task SeedOutboxMessageAsync(TenantCreatedEvent domainEvent)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = nameof(TenantCreatedEvent),
            Payload = JsonSerializer.Serialize(domainEvent),
            OccurredAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
