namespace Heum.Contracts.Events;

/// <summary>
/// Marks an event type as a domain event that can be raised by an aggregate root (or enqueued
/// ambiently via <c>Heum.Data.Domain.IDomainEventCollector</c>) and automatically dispatched to
/// Service Bus by <c>Heum.Data.Domain.DomainEventDispatchingInterceptor</c> after a successful
/// <c>SaveChanges</c>. In this app, domain events and Service Bus integration events are the same
/// types - see the "domain events" plan for the rationale.
/// </summary>
public interface IDomainEvent;
