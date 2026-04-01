using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IPasswordResetRepository : IRepository<PasswordReset>
{
    Task<PasswordReset?> GetByTokenHashAsync(string tokenHash);
    Task<IEnumerable<PasswordReset>> GetByUserIdAsync(Guid userId);
    /// <summary>
    /// Atomically mark a reset token as used. Returns false if the token was already consumed
    /// or has expired, preventing replay under concurrent requests.
    /// </summary>
    Task<bool> TryConsumeAsync(Guid id, DateTime usedAt);
}
