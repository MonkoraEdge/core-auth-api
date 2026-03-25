using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role> GetByIdAsync(Guid id);
    Task<Role> GetByRoleCodeAsync(string roleCode, Guid? tenantId = null);
    Task<IEnumerable<Role>> GetByTenantIdAsync(Guid? tenantId);
}
