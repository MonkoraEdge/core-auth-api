using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IUserSessionRepository : IRepository<UserSession>
{
    Task<UserSession?> GetByIdAsync(Guid id);
    Task<UserSession?> GetByTokenHashAsync(string sessionTokenHash);
    Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(Guid userId);
}
