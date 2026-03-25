using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IPasswordHistoryRepository : IRepository<PasswordHistory>
{
    Task<IEnumerable<PasswordHistory>> GetByUserIdAsync(Guid userId, int limit = 10);
}
