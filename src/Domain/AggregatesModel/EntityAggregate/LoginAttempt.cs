using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class LoginAttempt : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }

    public string? Username { get; set; }
    public Guid? ProviderId { get; set; }
    public string? LoginMethod { get; set; }

    public string IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }

    public string? CountryCode { get; set; }
    public string? City { get; set; }

    public bool Success { get; set; }
    public string? FailureReason { get; set; }

    public int? RiskScore { get; set; }
    public int? LatencyMs { get; set; }
}
