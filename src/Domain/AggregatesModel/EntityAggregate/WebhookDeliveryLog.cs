using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

/// <summary>
/// Persisted delivery attempt for a single webhook event.
/// Supports retry tracking and dead-letter analysis.
/// </summary>
public class WebhookDeliveryLog : BaseEntity
{
    public Guid WebhookEndpointId { get; set; }

    /// <summary>Event name (e.g. "user.created").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>JSON payload that was sent.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>HTTP status code returned by the target, or 0 on network failure.</summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>Whether this delivery was ultimately successful.</summary>
    public bool Success { get; set; }

    /// <summary>Number of attempts made (1 = first try, &gt;1 = retry).</summary>
    public int AttemptCount { get; set; } = 1;

    /// <summary>Error message if the delivery failed.</summary>
    public string? ErrorMessage { get; set; }
}
