using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;

public interface IRateLimitRepository : IRepository<RateLimit>
{
    Task<RateLimit> GetByIdentifierAndEndpointAsync(string identifier, string endpoint, DateTime windowStart);
    Task<bool> IsBlockedAsync(string identifier, string endpoint);
}
