using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IRevokedTokenRepository : IRepository<RevokedToken>
{
    Task<RevokedToken?> GetByTokenHashAsync(string tokenHash);
    Task<bool> IsRevokedAsync(string tokenHash);
}
