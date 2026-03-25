namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.DomainEvent
{
    /// <summary>
    /// Handler for a specific domain event type.
    /// Register implementations via <c>AddDomainEvents(Assembly)</c>.
    /// Multiple handlers for the same event type are all invoked.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type to handle.</typeparam>
    public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
