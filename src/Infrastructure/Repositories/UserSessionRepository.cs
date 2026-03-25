using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserSessionRepository : BaseRepository<AuthenticationDbContext, UserSession>, IUserSessionRepository
{
    public UserSessionRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<UserSession?> GetByIdAsync(Guid id) =>
        await Context.UserSessions.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<UserSession?> GetByTokenHashAsync(string tokenHash) =>
        await Context.UserSessions.FirstOrDefaultAsync(m => m.SessionTokenHash == tokenHash);

    public async Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(Guid userId) =>
        await Context.UserSessions.Where(m => m.UserId == userId && m.IsActive && m.ExpiresAt > DateTime.UtcNow).ToListAsync();
}
