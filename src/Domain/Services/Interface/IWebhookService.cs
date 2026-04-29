namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Enqueues webhook events for async delivery by <c>WebhookDeliveryBackgroundService</c>.
/// Events are pushed to a Redis list (simple queue) so delivery is decoupled from the request path.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Enqueue an event for delivery to all active endpoints subscribed to <paramref name="eventType"/>.
    /// </summary>
    Task EnqueueAsync(string eventType, object payload, CancellationToken ct = default);
}
