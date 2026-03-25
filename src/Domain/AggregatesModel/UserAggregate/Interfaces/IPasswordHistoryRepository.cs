using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IPasswordHistoryRepository : IRepository<PasswordHistory>
{
    Task<IEnumerable<PasswordHistory>> GetByUserIdAsync(Guid userId, int limit = 10);
}
