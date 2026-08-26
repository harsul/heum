namespace Heum.BackgroundService.Outbox;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    public TimeSpan PollingInterval { get; set; }

    public int BatchSize { get; set; }

    public int MaxAttempts { get; set; }

    public TimeSpan RetentionPeriod { get; set; }
}
