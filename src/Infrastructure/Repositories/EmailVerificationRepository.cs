using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class EmailVerificationRepository : AuthRepositoryBase<EmailVerification>, IEmailVerificationRepository
{
    public EmailVerificationRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<EmailVerification?> GetByTokenHashAsync(string tokenHash) =>
        await Context.EmailVerifications.FirstOrDefaultAsync(m => m.TokenHash == tokenHash);

    public async Task<IEnumerable<EmailVerification>> GetByUserIdAsync(Guid userId) =>
        await Context.EmailVerifications.Where(m => m.UserId == userId).ToListAsync();

    public async Task<bool> TryMarkVerifiedAsync(Guid id, DateTime verifiedAt)
    {
        var affected = await Context.EmailVerifications
            .Where(m => m.Id == id && m.VerifiedAt == null && m.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.VerifiedAt, verifiedAt));
        return affected == 1;
    }

    public async Task InvalidatePendingByUserIdAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        await Context.EmailVerifications
            .Where(m => m.UserId == userId && m.VerifiedAt == null && m.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.ExpiresAt, now));
    }
}
