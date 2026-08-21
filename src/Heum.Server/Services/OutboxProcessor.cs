using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Heum.Data;
using Heum.Data.Domain;
using Heum.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heum.Server.Services;

/// <inheritdoc cref="IOutboxProcessor" />
internal sealed class OutboxProcessor(
    HeumDbContext dbContext,
    IEventPublisher eventPublisher,
    OutboxEventTypeCatalog catalog,
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

        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.Attempts < opts.MaxAttempts)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(opts.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                var eventType = catalog.Resolve(message.EventType);
                var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType)
                    ?? throw new InvalidOperationException($"Outbox message '{message.Id}' deserialized to null.");

                var publishMethod = PublishMethods.GetOrAdd(
                    eventType,
                    t => PublishAsyncDefinition.MakeGenericMethod(t));

                await (Task)publishMethod.Invoke(eventPublisher, [domainEvent, cancellationToken])!;

                message.ProcessedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            }
            catch (Exception ex)
            {
                // MethodInfo.Invoke wraps exceptions thrown by the invoked method in a
                // TargetInvocationException, so unwrap it to surface the real failure.
                var actual = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;

                message.Attempts++;
                message.LastError = actual.Message.Length > 2000 ? actual.Message[..2000] : actual.Message;

                logger.LogError(
                    actual,
                    "Failed to publish outbox message {OutboxMessageId} ({EventType}), attempt {Attempts}/{MaxAttempts}.",
                    message.Id, message.EventType, message.Attempts, opts.MaxAttempts);
            }

            // Save after each message so one failure doesn't roll back the rest of the batch.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
