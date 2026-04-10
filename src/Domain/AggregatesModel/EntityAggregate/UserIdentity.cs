using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class UserIdentity : BaseEntity, ISoftDelete
{
    public Guid UserId { get; set; }

    public string ProviderType { get; set; } = "LOCAL";
    public string Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? PasswordAlgo { get; set; }
    public DateTime? PasswordUpdatedAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>Whether the identity is currently within a lockout window.</summary>
    public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;

    /// <summary>
    /// Records a failed authentication attempt and locks the identity once
    /// <paramref name="lockAfterAttempts"/> consecutive failures have occurred.
    /// </summary>
    public void RecordFailedAttempt(int lockAfterAttempts, int lockDurationMinutes)
    {
        FailedAttempts++;
        if (FailedAttempts >= lockAfterAttempts)
            LockedUntil = DateTime.UtcNow.AddMinutes(lockDurationMinutes);
    }

    /// <summary>Resets the failed-attempt counter and lifts any active lockout.</summary>
    public void ClearLock()
    {
        FailedAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>Updates the stored password credential to a new hash produced by the password service.</summary>
    public void SetPassword(string hash, string algo = "BCRYPT")
    {
        PasswordHash = hash;
        PasswordAlgo = algo;
        PasswordUpdatedAt = DateTime.UtcNow;
    }
}
