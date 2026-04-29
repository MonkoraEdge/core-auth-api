using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class UserTwoFactorRecoveryCode : BaseEntity, ISoftDelete
{
    public Guid UserId { get; set; }
    public Guid? TwoFactorSettingId { get; set; }

    public string CodeHash { get; set; }
    public string CodePrefix { get; set; }
    public Guid BatchId { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // ISoftDelete — allows tracking of deleted recovery code batches (e.g., on 2FA disable).
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
