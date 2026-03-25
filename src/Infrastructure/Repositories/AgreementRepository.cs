using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AgreementRepository : BaseRepository<AuthenticationDbContext, Agreement>, IAgreementRepository
{
    public AgreementRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<Agreement> GetByIdAsync(Guid id) =>
        await Context.Agreements.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Agreement> GetByAgreementCodeAsync(string agreementCode) =>
        await Context.Agreements.FirstOrDefaultAsync(m => m.AgreementCode == agreementCode);

    public async Task<IEnumerable<Agreement>> GetActiveByTenantAsync(Guid? tenantId) =>
        await Context.Agreements.Where(m => (tenantId == null || m.TenantId == tenantId) && m.IsActive).ToListAsync();

    public async Task<IEnumerable<Agreement>> GetActiveByTypeAsync(string agreementType, Guid? tenantId = null) =>
        await Context.Agreements.Where(m => m.AgreementType == agreementType && m.IsActive && (tenantId == null || m.TenantId == tenantId)).ToListAsync();
}
