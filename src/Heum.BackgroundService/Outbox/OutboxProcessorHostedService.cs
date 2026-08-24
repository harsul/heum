using Microsoft.Extensions.Options;

namespace Heum.BackgroundService.Outbox;

/// <summary>
/// Polls for pending <see cref="Heum.Data.Domain.OutboxMessage"/> rows on a timer and publishes
/// them via <see cref="IOutboxProcessor"/>. Kept deliberately thin - all the actual logic lives
/// in <see cref="OutboxProcessor"/> so it can be invoked directly (e.g. from tests) without
/// waiting on the poll interval.
/// </summary>
public sealed class OutboxProcessorHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessorOptions> options,
    ILogger<OutboxProcessorHostedService> logger) : Microsoft.Extensions.Hosting.BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                await processor.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing cycle failed.");
            }
        }
    }
}
