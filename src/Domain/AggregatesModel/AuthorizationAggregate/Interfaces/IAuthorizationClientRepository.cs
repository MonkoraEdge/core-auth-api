using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IAuthorizationClientRepository : IRepository<AuthorizationClient>
{
    Task<AuthorizationClient?> GetByIdAsync(Guid id);
    Task<AuthorizationClient?> GetByClientIdAsync(string clientId);
    Task<IEnumerable<AuthorizationClient>> GetByTenantIdAsync(Guid tenantId);
}
