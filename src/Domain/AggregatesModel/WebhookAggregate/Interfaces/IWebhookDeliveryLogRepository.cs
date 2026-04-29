using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.WebhookAggregate.Interfaces;

public interface IWebhookDeliveryLogRepository : IRepository<WebhookDeliveryLog>
{
    Task<IEnumerable<WebhookDeliveryLog>> GetByEndpointAsync(Guid endpointId, int limit = 50, CancellationToken ct = default);
}
