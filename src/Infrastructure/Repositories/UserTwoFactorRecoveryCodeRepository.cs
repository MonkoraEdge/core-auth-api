using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserTwoFactorRecoveryCodeRepository : AuthRepositoryBase<UserTwoFactorRecoveryCode>, IUserTwoFactorRecoveryCodeRepository
{
    public UserTwoFactorRecoveryCodeRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<UserTwoFactorRecoveryCode> GetByIdAsync(Guid id) =>
        await Context.UserTwoFactorRecoveryCodes.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<UserTwoFactorRecoveryCode> GetActiveByCodeHashAsync(string codeHash) =>
        await Context.UserTwoFactorRecoveryCodes.FirstOrDefaultAsync(m => m.CodeHash == codeHash && m.IsActive && m.UsedAt == null);

    public async Task<IEnumerable<UserTwoFactorRecoveryCode>> GetActiveByUserIdAsync(Guid userId) =>
        await Context.UserTwoFactorRecoveryCodes.Where(m => m.UserId == userId && m.IsActive && m.UsedAt == null).ToListAsync();
}
