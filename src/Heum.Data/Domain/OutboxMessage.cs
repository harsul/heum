namespace Heum.Data.Domain;

/// <summary>
/// A durable record of a domain event, written transactionally alongside the entity change that
/// raised it (see <see cref="DomainEventDispatchingInterceptor"/>). A separate poller (in
/// <c>Heum.Server</c>) reads unprocessed rows and publishes them to Service Bus, retrying on
/// failure - this is the standard "transactional outbox" pattern, so an event is never lost just
/// because the downstream publish call fails after the DB commit succeeds.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Short CLR type name of the domain event (e.g. "TenantCreatedEvent").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>JSON-serialized event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
