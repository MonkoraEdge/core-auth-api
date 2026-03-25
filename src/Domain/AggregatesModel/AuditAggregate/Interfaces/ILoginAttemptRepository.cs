using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;

public interface ILoginAttemptRepository : IRepository<LoginAttempt>
{
    Task<IEnumerable<LoginAttempt>> GetByUserIdAsync(Guid userId, int limit = 20);
    Task<int> CountFailedByIpAddressAsync(string ipAddress, DateTime since);
    Task<int> CountFailedByUsernameAsync(string username, DateTime since);
}
