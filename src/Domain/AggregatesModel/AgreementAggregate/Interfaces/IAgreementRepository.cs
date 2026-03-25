using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate.Interfaces;

public interface IAgreementRepository : IRepository<Agreement>
{
    Task<Agreement> GetByIdAsync(Guid id);
    Task<Agreement> GetByAgreementCodeAsync(string agreementCode);
    Task<IEnumerable<Agreement>> GetActiveByTenantAsync(Guid? tenantId);
    Task<IEnumerable<Agreement>> GetActiveByTypeAsync(string agreementType, Guid? tenantId = null);
}
