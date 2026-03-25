using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class RevokedTokenRepository : BaseRepository<AuthenticationDbContext, RevokedToken>, IRevokedTokenRepository
{
    public RevokedTokenRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<RevokedToken?> GetByTokenHashAsync(string tokenHash) =>
        await Context.RevokedTokens.FirstOrDefaultAsync(m => m.TokenHash == tokenHash);

    public async Task<bool> IsRevokedAsync(string tokenHash) =>
        await Context.RevokedTokens.AnyAsync(m => m.TokenHash == tokenHash);
}
