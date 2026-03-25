using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IUserTwoFactorRecoveryCodeRepository : IRepository<UserTwoFactorRecoveryCode>
{
    Task<UserTwoFactorRecoveryCode> GetByIdAsync(Guid id);
    Task<UserTwoFactorRecoveryCode> GetActiveByCodeHashAsync(string codeHash);
    Task<IEnumerable<UserTwoFactorRecoveryCode>> GetActiveByUserIdAsync(Guid userId);
}
