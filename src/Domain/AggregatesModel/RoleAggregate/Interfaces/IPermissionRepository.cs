using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<Permission> GetByIdAsync(Guid id);
    Task<Permission> GetByPermissionCodeAsync(string permissionCode, Guid? tenantId = null);
    Task<IEnumerable<Permission>> GetByTenantIdAsync(Guid? tenantId);
}
