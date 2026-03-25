using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IAccessTokenRepository : IRepository<AccessToken>
{
    Task<AccessToken> GetByTokenHashAsync(string tokenHash);
    Task<IEnumerable<AccessToken>> GetActiveByUserIdAsync(Guid userId);
    Task<IEnumerable<AccessToken>> GetActiveByClientIdAsync(Guid clientId);
}
