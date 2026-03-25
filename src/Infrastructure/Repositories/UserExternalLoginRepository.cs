using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserExternalLoginRepository : BaseRepository<AuthenticationDbContext, UserExternalLogin>, IUserExternalLoginRepository
{
    public UserExternalLoginRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<UserExternalLogin?> GetByIdAsync(Guid id) =>
        await Context.UserExternalLogins.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<UserExternalLogin?> GetByProviderUserIdAsync(Guid providerId, string providerUserId) =>
        await Context.UserExternalLogins.FirstOrDefaultAsync(m => m.ProviderId == providerId && m.ProviderUserId == providerUserId);

    public async Task<IEnumerable<UserExternalLogin>> GetByUserIdAsync(Guid userId) =>
        await Context.UserExternalLogins.Where(m => m.UserId == userId).ToListAsync();
}
