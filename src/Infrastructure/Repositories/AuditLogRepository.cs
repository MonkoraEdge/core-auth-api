using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.Auth.Infrastructure.Repositories;

public class AuditLogRepository : AuthRepositoryBase<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AuthenticationDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int limit = 50) =>
        await Context.AuditLogs.Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt).Take(limit).ToListAsync();

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid entityId) =>
        await Context.AuditLogs.Where(m => m.EntityName == entityName && m.EntityId == entityId).OrderByDescending(m => m.CreatedAt).ToListAsync();
}
