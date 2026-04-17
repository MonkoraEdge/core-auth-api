using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
    using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces
{
    public interface ITenantRepository : IRepository<Tenant>
    {
        Task<Tenant?> GetTenantByIdAsync(Guid id);
        Task<List<Tenant>> GetListAsync(string? keyword, bool? isActive);
    }
}
