using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
    using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces
{
    public interface ITenanttRepository : IRepository<Tenant>
    {
        Task<Tenant> GetTenantByIdAsync(Guid id);
    }
}
