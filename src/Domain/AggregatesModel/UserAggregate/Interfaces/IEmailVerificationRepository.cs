using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IEmailVerificationRepository : IRepository<EmailVerification>
{
    Task<EmailVerification?> GetByTokenHashAsync(string tokenHash);
    Task<IEnumerable<EmailVerification>> GetByUserIdAsync(Guid userId);
    /// <summary>
    /// Atomically mark a verification token as verified. Returns false if already verified
    /// or expired, preventing double-verification under concurrent requests.
    /// </summary>
    Task<bool> TryMarkVerifiedAsync(Guid id, DateTime verifiedAt);
    /// <summary>
    /// Expire all pending (unverified, not yet expired) tokens for a user. Called before
    /// issuing a new token so that old links sent by email cannot be replayed.
    /// </summary>
    Task InvalidatePendingByUserIdAsync(Guid userId);
}
