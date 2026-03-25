using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;

public interface IUserExternalLoginRepository : IRepository<UserExternalLogin>
{
    Task<UserExternalLogin> GetByIdAsync(Guid id);
    Task<UserExternalLogin> GetByProviderUserIdAsync(Guid providerId, string providerUserId);
    Task<IEnumerable<UserExternalLogin>> GetByUserIdAsync(Guid userId);
}
