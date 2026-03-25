using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IPasswordResetRepository : IRepository<PasswordReset>
{
    Task<PasswordReset> GetByTokenHashAsync(string tokenHash);
    Task<IEnumerable<PasswordReset>> GetByUserIdAsync(Guid userId);
}
