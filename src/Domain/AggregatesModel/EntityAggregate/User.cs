using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class User : BaseEntity, ISoftDelete, IAggregateRoot
{
    public Guid? TenantId { get; set; }

    public string? DisplayName { get; set; }
    public string? LocaleCode { get; set; }
    public string? Zoneinfo { get; set; }

    public string? PhoneNumber { get; set; }
    public bool PhoneVerified { get; set; }
    public string Email { get; set; }
    public bool EmailVerified { get; set; }

    public string Status { get; set; } = UserStatus.Inactive;
    public string RegistrationSource { get; set; } = "LOCAL";

    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? LastPasswordChangedAt { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>Updates the last-login and last-activity timestamps after a successful authentication.</summary>
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>Bumps the last-activity timestamp (e.g. on every authenticated API call).</summary>
    public void RecordActivity()
        => LastActivityAt = DateTime.UtcNow;

    /// <summary>Activates the account and transitions status to Active.</summary>
    public void Activate()
    {
        IsActive = true;
        Status = UserStatus.Active;
    }

    /// <summary>Suspends the account and marks it as inactive.</summary>
    public void Deactivate()
    {
        IsActive = false;
        Status = UserStatus.Suspended;
    }

    /// <summary>Marks the email as verified and promotes an Inactive account to Active.</summary>
    public void MarkEmailVerified()
    {
        EmailVerified = true;
        if (Status == UserStatus.Inactive)
            Status = UserStatus.Active;
    }

    /// <summary>Records the timestamp of a successful password change.</summary>
    public void RecordPasswordChanged() => LastPasswordChangedAt = DateTime.UtcNow;
}
