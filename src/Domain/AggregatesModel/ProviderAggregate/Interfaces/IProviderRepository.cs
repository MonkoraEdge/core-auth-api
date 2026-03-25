using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate.Interfaces;

public interface IProviderRepository : IRepository<Provider>
{
    Task<Provider> GetByIdAsync(Guid id);
    Task<Provider> GetByProviderCodeAsync(string providerCode);
    Task<IEnumerable<Provider>> GetAllActiveAsync();
}
