using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AuthorizationConsentRepository : BaseRepository<AuthenticationDbContext, AuthorizationConsent>, IAuthorizationConsentRepository
{
    public AuthorizationConsentRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<AuthorizationConsent?> GetActiveByUserAndClientAsync(Guid userId, Guid clientId) =>
        await Context.AuthorizationConsents.FirstOrDefaultAsync(m => m.UserId == userId && m.ClientId == clientId && (m.ExpiresAt == null || m.ExpiresAt > DateTime.UtcNow));

    public async Task<IEnumerable<AuthorizationConsent>> GetByUserIdAsync(Guid userId) =>
        await Context.AuthorizationConsents.Where(m => m.UserId == userId).ToListAsync();
}
