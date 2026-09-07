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

namespace Heum.BackgroundService.Outbox;

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
        // The whole fetch → publish → mark cycle runs inside ONE database transaction. That is
        // what makes "FOR UPDATE SKIP LOCKED" meaningful: the row locks taken by the SELECT are
        // held until Commit, so a second processor instance polling concurrently skips these rows
        // instead of publishing them a second time. Without the transaction the locks were
        // released as soon as the SELECT returned, which allowed duplicate publishes.
        //
        // Aspire enables a retrying execution strategy on the DbContext, and EF refuses
        // user-initiated transactions under a retrying strategy unless they are wrapped in
        // ExecuteAsync - so we go through the strategy explicitly.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await ProcessBatchAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        await CleanupProcessedAsync(cancellationToken);
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var pending = await FetchPendingAsync(opts.MaxAttempts, opts.BatchSize, now, cancellationToken);

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
                message.NextAttemptAtUtc = null;
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
                    message.NextAttemptAtUtc = null;
                    logger.LogCritical(
                        actual,
                        "Outbox message {OutboxMessageId} ({EventType}) permanently abandoned after {MaxAttempts} attempts. Last error: {LastError}",
                        message.Id, message.EventType, opts.MaxAttempts, message.LastError);
                }
                else
                {
                    var delay = ComputeBackoff(message.Attempts, opts);
                    message.NextAttemptAtUtc = timeProvider.GetUtcNow().UtcDateTime + delay;
                    logger.LogError(
                        actual,
                        "Failed to publish outbox message {OutboxMessageId} ({EventType}), attempt {Attempts}/{MaxAttempts}. Next attempt in {Delay}.",
                        message.Id, message.EventType, message.Attempts, opts.MaxAttempts, delay);
                }
            }

            // Flush per message so a failure recorded on one row is not lost if a later row throws
            // outside the try block; all flushes still commit together with the enclosing transaction.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>Exponential backoff: initial × 2^(attempts-1), capped at the configured maximum.</summary>
    internal static TimeSpan ComputeBackoff(int attempts, OutboxProcessorOptions opts)
    {
        var exponent = Math.Clamp(attempts - 1, 0, 30);
        var ticks = opts.InitialRetryDelay.Ticks * (1L << exponent);
        return ticks <= 0 || ticks > opts.MaxRetryDelay.Ticks ? opts.MaxRetryDelay : TimeSpan.FromTicks(ticks);
    }

    private Task<List<OutboxMessage>> FetchPendingAsync(int maxAttempts, int batchSize, DateTime now, CancellationToken cancellationToken)
    {
        if (IsInMemoryProvider())
        {
            return dbContext.OutboxMessages
                .Where(m => m.ProcessedAtUtc == null
                            && m.Attempts < maxAttempts
                            && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now))
                .OrderBy(m => m.OccurredAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        return dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedAtUtc" IS NULL
                  AND "Attempts" < {maxAttempts}
                  AND ("NextAttemptAtUtc" IS NULL OR "NextAttemptAtUtc" <= {now})
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
