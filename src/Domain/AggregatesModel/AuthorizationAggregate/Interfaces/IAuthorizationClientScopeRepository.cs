using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IAuthorizationClientScopeRepository : IRepository<AuthorizationClientScope>
{
    Task<IEnumerable<AuthorizationClientScope>> GetByClientIdAsync(Guid clientId);
}
