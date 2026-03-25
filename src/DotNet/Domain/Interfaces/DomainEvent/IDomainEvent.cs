namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.DomainEvent
{
    /// <summary>
    /// Marker interface for domain events.
    /// Implement this on any class that represents a domain event.
    /// Call <c>entity.AddDomainEvent(event)</c> and events are automatically dispatched
    /// via <see cref="IDomainEventDispatcher"/> after <c>BaseDbContext.SaveChangesAsync</c>.
    /// Register handlers with <c>services.AddDomainEvents(Assembly)</c>.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>Unique identifier for this event instance.</summary>
        Guid Id { get; }

        /// <summary>UTC timestamp when the event occurred.</summary>
        DateTime OccurredOn { get; }
    }
}
