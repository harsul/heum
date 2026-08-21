using Heum.Contracts.Events;

namespace Heum.Data.Domain;

/// <summary>
/// Implemented by aggregate roots (see <see cref="AggregateRoot"/>) that can accumulate domain
/// events to be dispatched by <see cref="DomainEventDispatchingInterceptor"/> after a successful
/// <c>SaveChanges</c>.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
