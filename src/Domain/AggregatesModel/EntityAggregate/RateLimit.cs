using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class RateLimit : BaseEntity
{
    public string Identifier { get; set; }
    public string Endpoint { get; set; }

    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }

    public int RequestCount { get; set; }
    public int LimitCount { get; set; }

    public DateTime? BlockedUntil { get; set; }
    public string Scope { get; set; }
    public DateTime ExpiresAt { get; set; }
}
