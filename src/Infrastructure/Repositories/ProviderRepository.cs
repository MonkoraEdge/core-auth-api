using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class ProviderRepository : AuthRepositoryBase<Provider>, IProviderRepository
{
    public ProviderRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<Provider?> GetByIdAsync(Guid id) =>
        await Context.Providers.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Provider?> GetByProviderCodeAsync(string providerCode) =>
        await Context.Providers.FirstOrDefaultAsync(m => m.ProviderCode == providerCode);

    public async Task<IEnumerable<Provider>> GetAllActiveAsync() =>
        await Context.Providers.Where(m => m.IsActive).ToListAsync();
}
