using MonkoraEdge.Core.DotNet.Domain.Interfaces.DomainEvent;
using Microsoft.Extensions.DependencyInjection;

namespace MonkoraEdge.Core.DotNet.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="IDomainEventDispatcher"/>.
    /// Resolves all <see cref="IDomainEventHandler{TEvent}"/> implementations at runtime
    /// using reflection to handle the concrete event type, and invokes them sequentially.
    ///
    /// Register via <c>AddDomainEvents(Assembly)</c>.
    /// <c>BaseDbContext</c> calls this automatically after <c>SaveChangesAsync</c>
    /// when <c>IDomainEventDispatcher</c> is injected.
    /// </summary>
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public DomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
                if (method != null)
                    await (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken });
            }
        }

        /// <inheritdoc />
        public async Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
                await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
