using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Heum.Server.Services;

/// <inheritdoc cref="IOutboxProcessor" />
internal sealed class OutboxProcessor(
    HeumDbContext dbContext,
    IEventPublisher eventPublisher,
    EventTopicRegistry registry,
    IOptions<OutboxProcessorOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger) : IOutboxProcessor
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> PublishMethods = new();
    private static readonly MethodInfo PublishAsyncDefinition =
        typeof(IEventPublisher).GetMethod(nameof(IEventPublisher.PublishAsync))!;

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        var pending = await FetchPendingAsync(opts.MaxAttempts, opts.BatchSize, cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                var eventType = registry.ResolveType(message.EventType);
                var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType)
                    ?? throw new InvalidOperationException($"Outbox message '{message.Id}' deserialized to null.");

                var publishMethod = PublishMethods.GetOrAdd(
                    eventType,
                    t => PublishAsyncDefinition.MakeGenericMethod(t));

                await (Task)publishMethod.Invoke(eventPublisher, [domainEvent, cancellationToken, message.Id.ToString()])!;

                message.ProcessedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            }
            catch (Exception ex)
            {
                // MethodInfo.Invoke wraps exceptions thrown by the invoked method in a
                // TargetInvocationException, so unwrap it to surface the real failure.
                var actual = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;

                message.Attempts++;
                message.LastError = actual.Message.Length > 2000 ? actual.Message[..2000] : actual.Message;

                if (message.Attempts >= opts.MaxAttempts)
                {
                    logger.LogCritical(
                        actual,
                        "Outbox message {OutboxMessageId} ({EventType}) permanently abandoned after {MaxAttempts} attempts. Last error: {LastError}",
                        message.Id, message.EventType, opts.MaxAttempts, message.LastError);
                }
                else
                {
                    logger.LogError(
                        actual,
                        "Failed to publish outbox message {OutboxMessageId} ({EventType}), attempt {Attempts}/{MaxAttempts}.",
                        message.Id, message.EventType, message.Attempts, opts.MaxAttempts);
                }
            }

            // Save after each message so one failure doesn't roll back the rest of the batch.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await CleanupProcessedAsync(cancellationToken);
    }

    private Task<List<OutboxMessage>> FetchPendingAsync(int maxAttempts, int batchSize, CancellationToken cancellationToken)
    {
        if (IsInMemoryProvider())
        {
            return dbContext.OutboxMessages
                .Where(m => m.ProcessedAtUtc == null && m.Attempts < maxAttempts)
                .OrderBy(m => m.OccurredAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        return dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedAtUtc" IS NULL AND "Attempts" < {maxAttempts}
                ORDER BY "OccurredAtUtc"
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
    }

    private async Task CleanupProcessedAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - options.Value.RetentionPeriod;

        if (IsInMemoryProvider())
        {
            var stale = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < cutoff)
                .ToListAsync(cancellationToken);

            if (stale.Count > 0)
            {
                dbContext.OutboxMessages.RemoveRange(stale);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Cleaned up {Count} processed outbox messages older than {Cutoff:u}.", stale.Count, cutoff);
            }

            return;
        }

        var deleted = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            logger.LogInformation("Cleaned up {Count} processed outbox messages older than {Cutoff:u}.", deleted, cutoff);
    }

    private bool IsInMemoryProvider() =>
        dbContext.Database.GetService<IDatabaseProvider>().Name
            .Contains("InMemory", StringComparison.OrdinalIgnoreCase);
}
