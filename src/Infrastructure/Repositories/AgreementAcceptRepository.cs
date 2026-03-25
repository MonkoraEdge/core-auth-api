using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AgreementAcceptRepository : AuthRepositoryBase<AgreementAccept>, IAgreementAcceptRepository
{
    public AgreementAcceptRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<AgreementAccept?> GetActiveByUserAndAgreementAsync(Guid userId, Guid agreementId) =>
        await Context.AgreementAccepts.FirstOrDefaultAsync(m => m.UserId == userId && m.AgreementId == agreementId && m.IsActive);

    public async Task<IEnumerable<AgreementAccept>> GetByUserIdAsync(Guid userId) =>
        await Context.AgreementAccepts.Where(m => m.UserId == userId).ToListAsync();

    public async Task<bool> HasAcceptedAsync(Guid userId, Guid agreementId) =>
        await Context.AgreementAccepts.AnyAsync(m => m.UserId == userId && m.AgreementId == agreementId && m.IsActive);
}
