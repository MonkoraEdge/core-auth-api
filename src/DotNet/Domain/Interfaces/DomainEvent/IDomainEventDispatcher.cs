namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.DomainEvent
{
    /// <summary>
    /// Dispatches domain events to all registered <see cref="IDomainEventHandler{TEvent}"/> implementations.
    /// Automatically invoked by <c>BaseDbContext.SaveChangesAsync</c> after a successful save,
    /// when registered via <c>AddDomainEvents(Assembly)</c>.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        /// <summary>Dispatch a single domain event to all matching handlers.</summary>
        Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

        /// <summary>Dispatch a list of domain events in order.</summary>
        Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
