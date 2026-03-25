using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class UserSessionDevice : BaseEntity
{
    public Guid UserId { get; set; }

    public string? DeviceName { get; set; }
    public string DeviceType { get; set; } = "UNKNOWN";
    public string? DeviceFingerprint { get; set; }

    public string? OsName { get; set; }
    public string? OsVersion { get; set; }

    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public string? LoginMethod { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }

    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsBlocked { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsActive { get; set; }
}
