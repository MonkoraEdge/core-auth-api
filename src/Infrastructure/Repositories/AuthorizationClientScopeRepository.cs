using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AuthorizationClientScopeRepository : AuthRepositoryBase<AuthorizationClientScope>, IAuthorizationClientScopeRepository
{
    public AuthorizationClientScopeRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<AuthorizationClientScope>> GetByClientIdAsync(Guid clientId) =>
        await Context.AuthorizationClientScopes.Where(m => m.ClientId == clientId).ToListAsync();
}
