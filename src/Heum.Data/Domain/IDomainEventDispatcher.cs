using Heum.Contracts.Events;

namespace Heum.Data.Domain;

/// <summary>
/// Publishes domain events collected by <see cref="DomainEventDispatchingInterceptor"/>. The
/// concrete implementation (e.g. one backed by Service Bus) is registered by the composition
/// root (<c>Heum.Server</c>) - <c>Heum.Data</c> deliberately has no dependency on any messaging
/// infrastructure.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
