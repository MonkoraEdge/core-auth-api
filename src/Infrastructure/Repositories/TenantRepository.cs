using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class TenantRepository : AuthRepositoryBase<Tenant>, ITenantRepository
{
    public TenantRepository(AuthenticationDbContext context)
        : base(context)
    {
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid id)
    {
        return await Context.Tenants.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Tenant>> GetListAsync(string? keyword, bool? isActive)
    {
        var query = Context.Tenants.Where(t => t.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(t =>
                t.TenantCode.Contains(keyword) ||
                (t.TenantName != null && (t.TenantName.EN.Contains(keyword) || t.TenantName.TH.Contains(keyword))));

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }
}