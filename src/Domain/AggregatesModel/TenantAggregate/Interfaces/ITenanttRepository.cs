using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces
{
    public interface ITenanttRepository : IRepository<Tenant>
    {
        Task<Tenant> GetTenantByIdAsync(Guid id);
    }
}
