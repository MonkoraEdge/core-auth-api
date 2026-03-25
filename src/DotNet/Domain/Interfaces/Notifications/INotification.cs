namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.Notifications
{
    /// <summary>
    /// Marker interface for in-process notifications (fan-out publish/subscribe).
    /// Unlike domain events (dispatched after DB save), notifications are published
    /// directly via <see cref="INotificationDispatcher"/> and handled by all
    /// registered <see cref="INotificationHandler{TNotification}"/> concurrently.
    ///
    /// Typical uses: send emails, push notifications, update read-models, trigger webhooks.
    /// </summary>
    public interface INotification { }
}
