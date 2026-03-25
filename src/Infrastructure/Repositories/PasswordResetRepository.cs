using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class PasswordResetRepository : AuthRepositoryBase<PasswordReset>, IPasswordResetRepository
{
    public PasswordResetRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<PasswordReset?> GetByTokenHashAsync(string tokenHash) =>
        await Context.PasswordResets.FirstOrDefaultAsync(m => m.TokenHash == tokenHash);

    public async Task<IEnumerable<PasswordReset>> GetByUserIdAsync(Guid userId) =>
        await Context.PasswordResets.Where(m => m.UserId == userId).ToListAsync();
}
