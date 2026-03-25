using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AccessToken : BaseEntity
{
    public string TokenHash { get; set; }
    public string TokenType { get; set; } = "Bearer";

    public Guid ClientId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }

    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string? GrantType { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
