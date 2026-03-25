namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.Notifications
{
    /// <summary>
    /// Dispatches a notification to all registered <see cref="INotificationHandler{TNotification}"/>
    /// implementations concurrently.
    /// Register via <c>AddNotifications(Assembly)</c>.
    /// </summary>
    public interface INotificationDispatcher
    {
        /// <summary>
        /// Publish a notification to all registered handlers.
        /// All handlers are invoked concurrently via <c>Task.WhenAll</c>.
        /// </summary>
        Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}
