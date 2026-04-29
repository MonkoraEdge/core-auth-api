using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.SamlAggregate.Interfaces;

public interface ISamlProviderRepository : IRepository<SamlProvider>
{
    Task<SamlProvider?> GetByIdAsync(Guid id);
    Task<SamlProvider?> GetByCodeAsync(string providerCode);
    Task<IEnumerable<SamlProvider>> GetAllActiveAsync();
}
