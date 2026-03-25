using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;

public interface IScopeRepository : IRepository<Scope>
{
    Task<Scope?> GetByIdAsync(Guid id);
    Task<Scope?> GetByScopeNameAsync(string scopeName);
    Task<IEnumerable<Scope>> GetAllActiveAsync();
    /// <summary>Batch-load scopes by a set of IDs in one query — avoids N+1 per client scope.</summary>
    Task<IEnumerable<Scope>> GetByIdsAsync(IEnumerable<Guid> ids);
}
