using Heum.Contracts.Events;

namespace Heum.Infrastructure.Messaging;

/// <summary>
/// The single place that declares which Service Bus topic each domain event is published to.
/// Both the API (which writes outbox rows) and the outbox processor (which publishes them) call
/// <see cref="MapDomainEvents"/>, so an event can never be enqueued by one side and unknown to
/// the other. Adding a new <see cref="IDomainEvent"/> means adding one line here; the
/// <c>EventTopicMapTests</c> unit test fails if an event in <c>Heum.Contracts</c> is missing.
/// </summary>
public static class EventTopicMap
{
    public const string TenantEvents = "tenant-events";
    public const string UserEvents = "user-events";

    public static EventTopicRegistry MapDomainEvents(this EventTopicRegistry registry) => registry
        .MapTopic<TenantCreatedEvent>(TenantEvents)
        .MapTopic<TenantPlanChangedEvent>(TenantEvents)
        .MapTopic<PlanEntitlementChangedEvent>(TenantEvents)
        .MapTopic<UserOnboardingRequestedEvent>(UserEvents)
        .MapTopic<InvitationCreatedEvent>(UserEvents);
}
