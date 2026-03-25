using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AuthorizationClientRepository : BaseRepository<AuthenticationDbContext, AuthorizationClient>, IAuthorizationClientRepository
{
    public AuthorizationClientRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<AuthorizationClient?> GetByIdAsync(Guid id) =>
        await Context.AuthorizationClients.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<AuthorizationClient?> GetByClientIdAsync(string clientId) =>
        await Context.AuthorizationClients.FirstOrDefaultAsync(m => m.ClientId == clientId);

    public async Task<IEnumerable<AuthorizationClient>> GetByTenantIdAsync(Guid tenantId) =>
        await Context.AuthorizationClients.Where(m => m.TenantId == tenantId).ToListAsync();
}
