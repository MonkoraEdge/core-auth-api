using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class PermissionRepository : BaseRepository<AuthenticationDbContext, Permission>, IPermissionRepository
{
    public PermissionRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<Permission> GetByIdAsync(Guid id) =>
        await Context.Permissions.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Permission> GetByPermissionCodeAsync(string permissionCode, Guid? tenantId = null) =>
        await Context.Permissions.FirstOrDefaultAsync(m => m.PermissionCode == permissionCode && (tenantId == null || m.TenantId == tenantId));

    public async Task<IEnumerable<Permission>> GetByTenantIdAsync(Guid? tenantId) =>
        await Context.Permissions.Where(m => tenantId == null || m.TenantId == tenantId).ToListAsync();
}
