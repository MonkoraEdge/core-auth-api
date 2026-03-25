using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class LoginAttemptRepository : AuthRepositoryBase<LoginAttempt>, ILoginAttemptRepository
{
    public LoginAttemptRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<LoginAttempt>> GetByUserIdAsync(Guid userId, int limit = 20) =>
        await Context.LoginAttempts.Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt).Take(limit).ToListAsync();

    public async Task<int> CountFailedByIpAddressAsync(string ipAddress, DateTime since) =>
        await Context.LoginAttempts.CountAsync(m => m.IpAddress == ipAddress && !m.Success && m.CreatedAt >= since);

    public async Task<int> CountFailedByUsernameAsync(string username, DateTime since) =>
        await Context.LoginAttempts.CountAsync(m => m.Username == username && !m.Success && m.CreatedAt >= since);
}
