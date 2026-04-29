using Microsoft.EntityFrameworkCore;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.SamlAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class SamlProviderRepository
    : AuthRepositoryBase<SamlProvider>, ISamlProviderRepository
{
    public SamlProviderRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<SamlProvider?> GetByIdAsync(Guid id) =>
        await Context.SamlProviders.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<SamlProvider?> GetByCodeAsync(string providerCode) =>
        await Context.SamlProviders
            .FirstOrDefaultAsync(p => p.ProviderCode == providerCode && p.IsActive);

    public async Task<IEnumerable<SamlProvider>> GetAllActiveAsync() =>
        await Context.SamlProviders
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayName)
            .ToListAsync();
}
