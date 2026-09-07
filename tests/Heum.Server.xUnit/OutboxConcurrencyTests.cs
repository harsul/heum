using System.Text.Json;
using Heum.BackgroundService.Outbox;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace Heum.Server.xUnit;

/// <summary>
/// Verifies that two concurrent <see cref="OutboxProcessor"/> instances each process every
/// outbox row exactly once, relying on PostgreSQL's <c>FOR UPDATE SKIP LOCKED</c>.
/// Skipped when <c>USE_TESTCONTAINERS</c> is not set; the InMemory provider has no
/// row-locking semantics and cannot verify this guarantee.
/// </summary>
public sealed class OutboxConcurrencyTests : IAsyncLifetime
{
    private static bool UseTestcontainers =>
        Environment.GetEnvironmentVariable("USE_TESTCONTAINERS") == "true"
        || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

    private PostgreSqlContainer? _postgres;

    public async ValueTask InitializeAsync()
    {
        if (!UseTestcontainers)
            return;

        _postgres = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
        await _postgres.StartAsync();

        await using var db = BuildDb(_postgres.GetConnectionString());
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task TwoProcessors_EachMessagePublishedExactlyOnce()
    {
        if (_postgres is null)
            return; // Skip: real Postgres required for FOR UPDATE SKIP LOCKED

        var connString = _postgres.GetConnectionString();

        // Seed 5 outbox messages.
        const int messageCount = 5;
        await using var seedDb = BuildDb(connString);
        for (var i = 0; i < messageCount; i++)
        {
            seedDb.OutboxMessages.Add(new OutboxMessage
            {
                EventType = nameof(TenantCreatedEvent),
                Payload = JsonSerializer.Serialize(
                    new TenantCreatedEvent(Guid.NewGuid(), $"tenant-{i}", DateTimeOffset.UtcNow)),
                OccurredAtUtc = DateTime.UtcNow,
            });
        }
        await seedDb.SaveChangesAsync();

        var published = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var publisher = new CollectingPublisher(published);
        var registry = new EventTopicRegistry().MapTopic<TenantCreatedEvent>("tenant-events");
        var opts = Options.Create(new OutboxProcessorOptions { MaxAttempts = 3 });

        // Two processors share the same Postgres DB but use independent DbContexts.
        await using var db1 = BuildDb(connString);
        await using var db2 = BuildDb(connString);

        var proc1 = new OutboxProcessor(db1, publisher, registry, opts, TimeProvider.System, NullLogger<OutboxProcessor>.Instance);
        var proc2 = new OutboxProcessor(db2, publisher, registry, opts, TimeProvider.System, NullLogger<OutboxProcessor>.Instance);

        await Task.WhenAll(
            proc1.ProcessPendingAsync(TestContext.Current.CancellationToken),
            proc2.ProcessPendingAsync(TestContext.Current.CancellationToken));

        // Each message must be published exactly once — no duplicates, no misses.
        Assert.Equal(messageCount, published.Count);
    }

    private static HeumDbContext BuildDb(string connectionString)
    {
        var opts = new DbContextOptionsBuilder<HeumDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new HeumDbContext(opts);
    }

    private sealed class CollectingPublisher(System.Collections.Concurrent.ConcurrentBag<Guid> bag) : IEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default, string? messageId = null)
            where TEvent : notnull
        {
            bag.Add(messageId is not null && Guid.TryParse(messageId, out var id) ? id : Guid.NewGuid());
            return Task.CompletedTask;
        }
    }
}
