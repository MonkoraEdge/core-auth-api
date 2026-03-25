using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IEmailVerificationRepository : IRepository<EmailVerification>
{
    Task<EmailVerification?> GetByTokenHashAsync(string tokenHash);
    Task<IEnumerable<EmailVerification>> GetByUserIdAsync(Guid userId);
}
