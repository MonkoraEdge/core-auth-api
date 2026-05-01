using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class RefreshToken : BaseEntity
{
    public string RefreshTokenHash { get; set; }
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }
    /// <summary>Family ID shared by all tokens in a rotation chain. Used to detect refresh token theft (reuse of already-rotated token).</summary>
    public Guid FamilyId { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public string[] Scopes { get; set; } = Array.Empty<string>();

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    /// <summary>
    /// The hard deadline for this entire rotation chain, set once at first issuance and
    /// inherited unchanged through every rotation. Prevents unlimited sliding-window renewal:
    /// even if the client rotates continuously, the chain expires at this fixed timestamp.
    /// </summary>
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>True when the token has been explicitly revoked (theft detection, logout, rotation).</summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>True when either the sliding window or the absolute chain lifetime has elapsed.</summary>
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow || AbsoluteExpiresAt <= DateTime.UtcNow;

    /// <summary>Whether this token can still be used to obtain a new access token.</summary>
    public bool IsActive  => !IsRevoked && !IsExpired;
}
