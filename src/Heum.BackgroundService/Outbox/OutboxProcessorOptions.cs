namespace Heum.BackgroundService.Outbox;

/// <summary>
/// Configurable via an "OutboxProcessor" section in appsettings (falls back to these defaults).
/// </summary>
public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    public int BatchSize { get; set; } = 50;

    public int MaxAttempts { get; set; } = 5;

    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
}
