using Microsoft.EntityFrameworkCore;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.WebhookAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class WebhookEndpointRepository : AuthRepositoryBase<WebhookEndpoint>, IWebhookEndpointRepository
{
    public WebhookEndpointRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<WebhookEndpoint>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default) =>
        await Context.WebhookEndpoints
            .Where(e => e.ClientId == clientId && !e.IsDeleted)
            .ToListAsync(ct);

    public async Task<IEnumerable<WebhookEndpoint>> GetActiveByEventAsync(string eventType, CancellationToken ct = default) =>
        await Context.WebhookEndpoints
            .Where(e => e.IsActive && !e.IsDeleted &&
                        (e.Events == "*" || e.Events.Contains(eventType)))
            .ToListAsync(ct);
}
