using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class ApiKey : BaseEntity, ISoftDelete
{
    public Guid? TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? UserId { get; set; }

    public string KeyHash { get; set; }
    public string KeyPrefix { get; set; }
    public string KeyName { get; set; }
    public string[]? Scopes { get; set; }
    public string[]? AllowedIps { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>Revokes and soft-deletes the key atomically so it can never be used again.</summary>
    public void Revoke(string? revokedBy)
    {
        RevokedAt = DateTime.UtcNow;
        IsActive  = false;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = revokedBy;
    }

    /// <summary>Stamps the current UTC time as the last-used timestamp for auditing.</summary>
    public void RecordUsage() => LastUsedAt = DateTime.UtcNow;
}
