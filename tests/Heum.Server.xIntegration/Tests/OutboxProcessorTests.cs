using System.Text.Json;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Server.Services;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heum.Server.xIntegration.Tests;

/// <summary>
/// Exercises <see cref="IOutboxProcessor"/> directly against seeded <see cref="OutboxMessage"/>
/// rows, independent of any particular endpoint - see <see cref="TenantRegistrationTests"/> for
/// the end-to-end "an API call produces outbox rows" coverage.
/// </summary>
[Collection(nameof(IntegrationCollection))]
public class OutboxProcessorTests(IntegrationFixture fixture) : IAsyncLifetime
{
    private const int DefaultMaxAttempts = 5;

    async ValueTask IAsyncLifetime.InitializeAsync() =>
        await fixture.ResetDatabaseAsync();

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ProcessPendingAsync_PublishesAndMarksProcessed_ForAPendingMessage()
    {
        var tenantId = Guid.NewGuid();
        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            tenantId, "acme", "admin@acme.com", "kc-1", DateTimeOffset.UtcNow));

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var message = await db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(message.ProcessedAtUtc);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.LastError);

        var published = Assert.Single(fixture.FakeEvents.PublishedEvents);
        var publishedEvent = Assert.IsType<TenantCreatedEvent>(published);
        Assert.Equal(tenantId, publishedEvent.TenantId);
    }

    [Fact]
    public async Task ProcessPendingAsync_StopsRetrying_OnceMaxAttemptsIsReached()
    {
        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "acme", "admin@acme.com", "kc-1", DateTimeOffset.UtcNow));

        fixture.FakeEvents.ExceptionToThrow = new InvalidOperationException("Service Bus is unreachable");

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        for (var attempt = 1; attempt <= DefaultMaxAttempts; attempt++)
        {
            await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

            var message = await db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(attempt, message.Attempts);
            Assert.Null(message.ProcessedAtUtc);
            Assert.Contains("Service Bus is unreachable", message.LastError);
        }

        // One more cycle: the row now has Attempts == MaxAttempts, so it's excluded from the
        // query entirely - Attempts must stay exactly at the cap even though publishing would
        // still fail if it were retried.
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var finalMessage = await db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DefaultMaxAttempts, finalMessage.Attempts);
        Assert.Null(finalMessage.ProcessedAtUtc);

        // Even if the downstream issue is later fixed, a message that's exhausted its attempts
        // is left alone rather than silently retried forever.
        fixture.FakeEvents.ExceptionToThrow = null;
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var afterFixMessage = await db.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DefaultMaxAttempts, afterFixMessage.Attempts);
        Assert.Null(afterFixMessage.ProcessedAtUtc);
        Assert.Empty(fixture.FakeEvents.PublishedEvents);
    }

    [Fact]
    public async Task ProcessPendingAsync_DoesNotReprocess_AlreadyProcessedMessages()
    {
        await SeedOutboxMessageAsync(new TenantCreatedEvent(
            Guid.NewGuid(), "acme", "admin@acme.com", "kc-1", DateTimeOffset.UtcNow));

        await using var scope = fixture.Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        Assert.Single(fixture.FakeEvents.PublishedEvents);
    }

    private async Task SeedOutboxMessageAsync(TenantCreatedEvent domainEvent)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = nameof(TenantCreatedEvent),
            Payload = JsonSerializer.Serialize(domainEvent),
            OccurredAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
