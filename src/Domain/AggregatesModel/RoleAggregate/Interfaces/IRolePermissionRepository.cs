using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;

public interface IRolePermissionRepository : IRepository<RolePermission>
{
    Task<IEnumerable<RolePermission>> GetByRoleIdAsync(Guid roleId);
    Task<IEnumerable<RolePermission>> GetByPermissionIdAsync(Guid permissionId);
}
