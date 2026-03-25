using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IUserIdentityRepository : IRepository<UserIdentity>
{
    Task<UserIdentity> GetByIdAsync(Guid id);
    Task<UserIdentity> GetByUsernameAsync(string username);
    Task<UserIdentity> GetByUserIdAsync(Guid userId);
}
