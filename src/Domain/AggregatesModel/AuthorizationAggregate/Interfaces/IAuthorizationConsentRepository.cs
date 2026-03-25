using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IAuthorizationConsentRepository : IRepository<AuthorizationConsent>
{
    Task<AuthorizationConsent?> GetActiveByUserAndClientAsync(Guid userId, Guid clientId);
    Task<IEnumerable<AuthorizationConsent>> GetByUserIdAsync(Guid userId);
}
