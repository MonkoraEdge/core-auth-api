namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.Notifications
{
    /// <summary>
    /// Handler for a specific notification type.
    /// Multiple handlers for the same notification are all invoked in parallel.
    /// Register via <c>AddNotifications(Assembly)</c>.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to handle.</typeparam>
    public interface INotificationHandler<TNotification> where TNotification : INotification
    {
        Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
    }
}
