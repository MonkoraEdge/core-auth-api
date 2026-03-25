using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AuthorizationCode : BaseEntity
{
    public string CodeHash { get; set; }
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }

    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string RedirectUri { get; set; }

    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }

    public string? Nonce { get; set; }
    public DateTime? AuthTime { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
