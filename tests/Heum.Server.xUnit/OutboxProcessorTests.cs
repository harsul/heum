using System.Text.Json;
using Heum.BackgroundService.Outbox;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Infrastructure.Messaging;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
            // The processor holds a transaction across the batch; the in-memory provider
            // doesn't support them and throws unless told to ignore them.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var processor = CreateProcessor(fakeTime, _options);

        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "acme", DateTimeOffset.UtcNow));

        _events.ExceptionToThrow = new InvalidOperationException("Service Bus is unreachable");

        for (var attempt = 1; attempt <= DefaultMaxAttempts; attempt++)
        {
            await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

            var message = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(attempt, message.Attempts);
            Assert.Null(message.ProcessedAtUtc);
            Assert.Contains("Service Bus is unreachable", message.LastError);

            // Skip past the backoff window so the next cycle is allowed to retry.
            fakeTime.Advance(_options.MaxRetryDelay);
        }

        // One more cycle: the row now has Attempts == MaxAttempts, so it's excluded from the
        // query entirely - Attempts must stay exactly at the cap even though publishing would
        // still fail if it were retried.
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var finalMessage = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DefaultMaxAttempts, finalMessage.Attempts);
        Assert.Null(finalMessage.ProcessedAtUtc);

        // Even if the downstream issue is later fixed, a message that's exhausted its attempts
        // is left alone rather than silently retried forever.
        _events.ExceptionToThrow = null;
        fakeTime.Advance(_options.MaxRetryDelay);
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var afterFixMessage = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DefaultMaxAttempts, afterFixMessage.Attempts);
        Assert.Null(afterFixMessage.ProcessedAtUtc);
        Assert.Empty(_events.PublishedEvents);
    }

    [Fact]
    public async Task ProcessPendingAsync_DefersRetry_WithExponentialBackoff()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var options = new OutboxProcessorOptions
        {
            MaxAttempts = DefaultMaxAttempts,
            InitialRetryDelay = TimeSpan.FromSeconds(10),
            MaxRetryDelay = TimeSpan.FromMinutes(10),
        };
        var processor = CreateProcessor(fakeTime, options);

        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "acme", DateTimeOffset.UtcNow));
        _events.ExceptionToThrow = new InvalidOperationException("boom");

        // Attempt 1 fails → next attempt scheduled 10s out.
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
        var message = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, message.Attempts);
        Assert.Equal(fakeTime.GetUtcNow().UtcDateTime.AddSeconds(10), message.NextAttemptAtUtc);

        // Polling again before the backoff has elapsed must not touch the row.
        fakeTime.Advance(TimeSpan.FromSeconds(5));
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
        message = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, message.Attempts);

        // Once eligible, attempt 2 fails → delay doubles to 20s.
        fakeTime.Advance(TimeSpan.FromSeconds(5));
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
        message = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, message.Attempts);
        Assert.Equal(fakeTime.GetUtcNow().UtcDateTime.AddSeconds(20), message.NextAttemptAtUtc);

        // Recovery: a successful publish clears the schedule and marks the row processed.
        _events.ExceptionToThrow = null;
        fakeTime.Advance(TimeSpan.FromSeconds(20));
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
        message = await _db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(message.ProcessedAtUtc);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Single(_events.PublishedEvents);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 40)]
    [InlineData(4, 80)]
    [InlineData(10, 600)]   // capped at MaxRetryDelay (10 min)
    [InlineData(40, 600)]   // exponent clamp keeps the shift from overflowing
    public void ComputeBackoff_DoublesUntilCapped(int attempts, int expectedSeconds)
    {
        var options = new OutboxProcessorOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(10),
            MaxRetryDelay = TimeSpan.FromMinutes(10),
        };

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), OutboxProcessor.ComputeBackoff(attempts, options));
    }

    private OutboxProcessor CreateProcessor(TimeProvider timeProvider, OutboxProcessorOptions options) =>
        new(_db, _events, _registry, Options.Create(options), timeProvider, NullLogger<OutboxProcessor>.Instance);

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
