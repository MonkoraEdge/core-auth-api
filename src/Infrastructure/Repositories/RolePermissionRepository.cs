using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class RolePermissionRepository : AuthRepositoryBase<RolePermission>, IRolePermissionRepository
{
    public RolePermissionRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<RolePermission>> GetByRoleIdAsync(Guid roleId) =>
        await Context.RolePermissions.Where(m => m.RoleId == roleId).ToListAsync();

    public async Task<IEnumerable<RolePermission>> GetByPermissionIdAsync(Guid permissionId) =>
        await Context.RolePermissions.Where(m => m.PermissionId == permissionId).ToListAsync();
}
