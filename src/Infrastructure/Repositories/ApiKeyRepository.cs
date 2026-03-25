using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class ApiKeyRepository : AuthRepositoryBase<ApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<ApiKey?> GetByIdAsync(Guid id) =>
        await Context.ApiKeys.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash) =>
        await Context.ApiKeys.FirstOrDefaultAsync(m => m.KeyHash == keyHash);

    public async Task<IEnumerable<ApiKey>> GetByClientIdAsync(Guid clientId) =>
        await Context.ApiKeys.Where(m => m.ClientId == clientId).ToListAsync();

    public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId) =>
        await Context.ApiKeys.Where(m => m.UserId == userId).ToListAsync();
}
