using Microsoft.EntityFrameworkCore;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.WebhookAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class WebhookDeliveryLogRepository : AuthRepositoryBase<WebhookDeliveryLog>, IWebhookDeliveryLogRepository
{
    public WebhookDeliveryLogRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<WebhookDeliveryLog>> GetByEndpointAsync(
        Guid endpointId, int limit = 50, CancellationToken ct = default) =>
        await Context.WebhookDeliveryLogs
            .Where(l => l.WebhookEndpointId == endpointId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
}
