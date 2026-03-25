using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate.Interfaces;

public interface IApiKeyRepository : IRepository<ApiKey>
{
    Task<ApiKey?> GetByIdAsync(Guid id);
    Task<ApiKey?> GetByKeyHashAsync(string keyHash);
    Task<IEnumerable<ApiKey>> GetByClientIdAsync(Guid clientId);
    Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId);
}
