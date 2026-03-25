using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class PasswordHistoryRepository : AuthRepositoryBase<PasswordHistory>, IPasswordHistoryRepository
{
    public PasswordHistoryRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<PasswordHistory>> GetByUserIdAsync(Guid userId, int limit) =>
        await Context.PasswordHistories.Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();
}
