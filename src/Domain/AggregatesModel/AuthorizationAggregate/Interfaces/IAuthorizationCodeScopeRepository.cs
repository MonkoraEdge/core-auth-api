using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IAuthorizationCodeScopeRepository : IRepository<AuthorizationCodeScope>
{
    Task<IEnumerable<AuthorizationCodeScope>> GetByAuthorizationCodeIdAsync(Guid authorizationCodeId);
}
