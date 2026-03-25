using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AccessTokenRepository : BaseRepository<AuthenticationDbContext, AccessToken>, IAccessTokenRepository
{
    public AccessTokenRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<AccessToken?> GetByTokenHashAsync(string tokenHash) =>
        await Context.AccessTokens.FirstOrDefaultAsync(m => m.TokenHash == tokenHash);

    public async Task<IEnumerable<AccessToken>> GetActiveByUserIdAsync(Guid userId) =>
        await Context.AccessTokens.Where(m => m.UserId == userId && m.ExpiresAt > DateTime.UtcNow && m.RevokedAt == null).ToListAsync();

    public async Task<IEnumerable<AccessToken>> GetActiveByClientIdAsync(Guid clientId) =>
        await Context.AccessTokens.Where(m => m.ClientId == clientId && m.ExpiresAt > DateTime.UtcNow && m.RevokedAt == null).ToListAsync();
}
