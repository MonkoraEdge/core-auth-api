using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class RevokedToken : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }

    public string TokenHash { get; set; }
    public string TokenType { get; set; }
    public string? Reason { get; set; }

    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}
