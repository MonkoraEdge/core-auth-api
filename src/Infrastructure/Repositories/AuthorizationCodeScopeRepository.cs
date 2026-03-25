using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AuthorizationCodeScopeRepository : BaseRepository<AuthenticationDbContext, AuthorizationCodeScope>, IAuthorizationCodeScopeRepository
{
    public AuthorizationCodeScopeRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<AuthorizationCodeScope>> GetByAuthorizationCodeIdAsync(Guid authorizationCodeId) =>
        await Context.AuthorizationCodeScopes.Where(m => m.AuthorizationCodeId == authorizationCodeId).ToListAsync();
}
