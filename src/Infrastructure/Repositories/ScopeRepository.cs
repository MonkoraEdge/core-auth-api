using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class ScopeRepository : AuthRepositoryBase<Scope>, IScopeRepository
{
    public ScopeRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<Scope?> GetByIdAsync(Guid id) =>
        await Context.Scopes.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Scope?> GetByScopeNameAsync(string scopeName) =>
        await Context.Scopes.FirstOrDefaultAsync(m => m.ScopeName == scopeName);

    public async Task<IEnumerable<Scope>> GetAllActiveAsync() =>
        await Context.Scopes.Where(m => m.IsActive).ToListAsync();

    public async Task<IEnumerable<Scope>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList(); // materialise before LINQ-to-SQL
        return await Context.Scopes.Where(s => idList.Contains(s.Id)).ToListAsync();
    }
}
