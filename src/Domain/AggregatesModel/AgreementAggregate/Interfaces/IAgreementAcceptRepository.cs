using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate.Interfaces;

public interface IAgreementAcceptRepository : IRepository<AgreementAccept>
{
    Task<AgreementAccept> GetActiveByUserAndAgreementAsync(Guid userId, Guid agreementId);
    Task<IEnumerable<AgreementAccept>> GetByUserIdAsync(Guid userId);
    Task<bool> HasAcceptedAsync(Guid userId, Guid agreementId);
}
