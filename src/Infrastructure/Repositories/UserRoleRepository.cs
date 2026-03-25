using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class UserRoleRepository : AuthRepositoryBase<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId) =>
        await Context.UserRoles.Where(m => m.UserId == userId).ToListAsync();

    public async Task<IEnumerable<UserRole>> GetByRoleIdAsync(Guid roleId) =>
        await Context.UserRoles.Where(m => m.RoleId == roleId).ToListAsync();
}
