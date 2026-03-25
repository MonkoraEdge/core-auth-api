using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class EmailVerificationRepository : BaseRepository<AuthenticationDbContext, EmailVerification>, IEmailVerificationRepository
{
    public EmailVerificationRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<EmailVerification?> GetByTokenHashAsync(string tokenHash) =>
        await Context.EmailVerifications.FirstOrDefaultAsync(m => m.TokenHash == tokenHash);

    public async Task<IEnumerable<EmailVerification>> GetByUserIdAsync(Guid userId) =>
        await Context.EmailVerifications.Where(m => m.UserId == userId).ToListAsync();
}
