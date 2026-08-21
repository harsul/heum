namespace Heum.Server.Services;

/// <summary>
/// Publishes pending <see cref="Heum.Data.Domain.OutboxMessage"/> rows to Service Bus. Split out
/// from <see cref="OutboxProcessorHostedService"/> (which just loops on a timer) so it can be
/// invoked directly/deterministically, e.g. from tests, instead of waiting on the poll interval.
/// </summary>
public interface IOutboxProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}
