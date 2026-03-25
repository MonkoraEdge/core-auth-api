using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class RoleRepository : AuthRepositoryBase<Role>, IRoleRepository
{
    public RoleRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<Role> GetByIdAsync(Guid id) =>
        await Context.Roles.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Role> GetByRoleCodeAsync(string roleCode, Guid? tenantId = null) =>
        await Context.Roles.FirstOrDefaultAsync(m => m.RoleCode == roleCode && (tenantId == null || m.TenantId == tenantId));

    public async Task<IEnumerable<Role>> GetByTenantIdAsync(Guid? tenantId) =>
        await Context.Roles.Where(m => tenantId == null || m.TenantId == tenantId).ToListAsync();
}
