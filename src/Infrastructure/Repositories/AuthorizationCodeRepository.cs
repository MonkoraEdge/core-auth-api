using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AuthorizationCodeRepository : BaseRepository<AuthenticationDbContext, AuthorizationCode>, IAuthorizationCodeRepository
{
    public AuthorizationCodeRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<AuthorizationCode?> GetByCodeHashAsync(string codeHash) =>
        await Context.AuthorizationCodes.FirstOrDefaultAsync(m => m.CodeHash == codeHash);

    public async Task<IEnumerable<AuthorizationCode>> GetByUserIdAsync(Guid userId) =>
        await Context.AuthorizationCodes.Where(m => m.UserId == userId).ToListAsync();
}
