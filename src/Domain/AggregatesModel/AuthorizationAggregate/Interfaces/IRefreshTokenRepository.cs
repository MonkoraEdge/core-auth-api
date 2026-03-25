using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken> GetByTokenHashAsync(string refreshTokenHash);
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
    /// <summary>Returns all tokens belonging to the same rotation family (for theft detection/revocation)</summary>
    Task<IEnumerable<RefreshToken>> GetByFamilyIdAsync(Guid familyId);
}
