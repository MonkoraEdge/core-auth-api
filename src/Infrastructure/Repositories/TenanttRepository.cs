using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class TenanttRepository : AuthRepositoryBase<Tenant>, ITenanttRepository
{
    public TenanttRepository(AuthenticationDbContext context) 
        : base(context)
    {

    }
    
    public async Task<Tenant> GetTenantByIdAsync(Guid id)
    {
        return await Context.Tenants.FirstOrDefaultAsync(m => m.Id.Equals(id));
    }
   
}