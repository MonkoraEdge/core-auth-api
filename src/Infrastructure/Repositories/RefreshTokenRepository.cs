using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class RefreshTokenRepository : AuthRepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) =>
        await Context.RefreshTokens.FirstOrDefaultAsync(m => m.RefreshTokenHash == tokenHash);

    public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId) =>
        await Context.RefreshTokens.Where(m => m.UserId == userId && m.ExpiresAt > DateTime.UtcNow && m.RevokedAt == null).ToListAsync();

    public async Task<IEnumerable<RefreshToken>> GetByFamilyIdAsync(Guid familyId) =>
        await Context.RefreshTokens.Where(m => m.FamilyId == familyId).ToListAsync();
}
