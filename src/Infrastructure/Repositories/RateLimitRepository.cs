using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class RateLimitRepository : AuthRepositoryBase<RateLimit>, IRateLimitRepository
{
    public RateLimitRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<RateLimit> GetByIdentifierAndEndpointAsync(string identifier, string endpoint, DateTime windowStart) =>
        await Context.RateLimits.FirstOrDefaultAsync(m => m.Identifier == identifier && m.Endpoint == endpoint && m.WindowStart >= windowStart);

    public async Task<bool> IsBlockedAsync(string identifier, string endpoint) =>
        await Context.RateLimits.AnyAsync(m => m.Identifier == identifier && m.Endpoint == endpoint && m.BlockedUntil != null && m.BlockedUntil > DateTime.UtcNow);
}
