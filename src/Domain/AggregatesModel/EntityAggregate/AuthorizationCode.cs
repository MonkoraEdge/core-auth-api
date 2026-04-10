using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AuthorizationCode : BaseEntity
{
    public string CodeHash { get; set; }
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }

    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string RedirectUri { get; set; }

    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }

    public string? Nonce { get; set; }
    public DateTime? AuthTime { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>True once the code has been exchanged for tokens.</summary>
    public bool IsConsumed => ConsumedAt.HasValue;

    /// <summary>True when the code's one-time validity window has elapsed.</summary>
    public bool IsExpired  => ExpiresAt <= DateTime.UtcNow;

    /// <summary>True when the code may still be exchanged (not consumed and not expired).</summary>
    public bool IsValid    => !IsConsumed && !IsExpired;

    /// <summary>
    /// Marks the code as consumed. Idempotent — only the first call records the timestamp.
    /// An atomic database-level consume via <c>TryConsumeAsync</c> must still be performed
    /// to guard against concurrent replay.
    /// </summary>
    public void Consume()
    {
        if (!IsConsumed)
            ConsumedAt = DateTime.UtcNow;
    }
}
