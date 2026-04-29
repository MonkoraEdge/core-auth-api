using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

/// <summary>
/// A registered webhook endpoint belonging to an OAuth2 client.
/// Deliveries are signed with HMAC-SHA256 using <see cref="Secret"/>.
/// </summary>
public class WebhookEndpoint : BaseEntity, ISoftDelete
{
    /// <summary>The OAuth2 client that owns this endpoint.</summary>
    public Guid ClientId { get; set; }

    /// <summary>Target URL that receives POST requests.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Comma-separated list of event types to deliver (e.g. "user.created,token.issued").</summary>
    public string Events { get; set; } = string.Empty;

    /// <summary>Raw HMAC-SHA256 signing secret. Store encrypted at rest in production.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Whether this endpoint is active.</summary>
    public bool IsActive { get; set; } = true;

    // ISoftDelete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
