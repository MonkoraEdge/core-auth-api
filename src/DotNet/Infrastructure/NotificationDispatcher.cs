using MonkoraEdge.Core.DotNet.Domain.Interfaces.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace MonkoraEdge.Core.DotNet.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="INotificationDispatcher"/>.
    /// Resolves all <see cref="INotificationHandler{TNotification}"/> implementations for the
    /// published notification type and invokes them concurrently via <c>Task.WhenAll</c>.
    ///
    /// Register via <c>AddNotifications(Assembly)</c>.
    /// </summary>
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public NotificationDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            var handlerType = typeof(INotificationHandler<TNotification>);
            var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();

            var tasks = handlers.Select(h => h.HandleAsync(notification, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}
