using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IAuthorizationCodeRepository : IRepository<AuthorizationCode>
{
    Task<AuthorizationCode> GetByCodeHashAsync(string codeHash);
    Task<IEnumerable<AuthorizationCode>> GetByUserIdAsync(Guid userId);
}
