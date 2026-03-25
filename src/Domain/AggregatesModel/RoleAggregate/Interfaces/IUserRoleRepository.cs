using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;

public interface IUserRoleRepository : IRepository<UserRole>
{
    Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<UserRole>> GetByRoleIdAsync(Guid roleId);
}
