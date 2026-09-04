using System.ComponentModel.DataAnnotations;

namespace Heum.BackgroundService.Outbox;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 50;

    [Range(1, 100)]
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Delay before the first retry of a failed message. Each subsequent retry doubles the delay
    /// (exponential backoff) up to <see cref="MaxRetryDelay"/>, so a flapping broker isn't hammered
    /// on every poll.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
}
