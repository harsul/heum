using Heum.Contracts.Events;

namespace Heum.Data.Domain;

/// <summary>
/// Ambient, scoped queue for domain events that aren't tied to any aggregate's state change
/// (e.g. <c>UserOnboardingRequestedEvent</c>, which is raised as a side effect of a successful
/// Keycloak API call, not a DB column change). Flushed by
/// <see cref="DomainEventDispatchingInterceptor"/> on the next <c>SaveChanges</c>, alongside any
/// events raised directly on tracked aggregates.
/// </summary>
public interface IDomainEventCollector
{
    void Enqueue(IDomainEvent domainEvent);

    IReadOnlyList<IDomainEvent> DequeueAll();
}
