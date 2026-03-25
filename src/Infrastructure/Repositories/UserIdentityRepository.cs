using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserIdentityRepository : BaseRepository<AuthenticationDbContext, UserIdentity>, IUserIdentityRepository
{
    public UserIdentityRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<UserIdentity> GetByIdAsync(Guid id) =>
        await Context.UserIdentities.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<UserIdentity> GetByUsernameAsync(string username) =>
        await Context.UserIdentities.FirstOrDefaultAsync(m => m.Username == username);

    public async Task<UserIdentity> GetByUserIdAsync(Guid userId) =>
        await Context.UserIdentities.FirstOrDefaultAsync(m => m.UserId == userId);
}
