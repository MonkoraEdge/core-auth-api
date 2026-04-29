using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Repositories;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.WebhookAggregate.Interfaces;

public interface IWebhookEndpointRepository : IRepository<WebhookEndpoint>
{
    Task<IEnumerable<WebhookEndpoint>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);
    Task<IEnumerable<WebhookEndpoint>> GetActiveByEventAsync(string eventType, CancellationToken ct = default);
}
