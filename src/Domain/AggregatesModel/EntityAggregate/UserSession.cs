using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class UserSession : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }

    public string SessionTokenHash { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive { get; set; }
}
