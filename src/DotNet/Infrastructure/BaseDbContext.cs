using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.Interfaces.DomainEvent;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MonkoraEdge.Core.DotNet.Infrastructure
{
    /// <summary>
    /// Base DbContext for Code-First EF Core projects.
    /// Automatically handles:
    ///  - Audit fields (CreatedAt/By, UpdatedAt/By) on every SaveChanges
    ///  - Soft-delete interception (entities implementing ISoftDelete are marked instead of hard-deleted)
    ///  - Domain event dispatching after successful SaveChangesAsync (optional)
    ///  - Audit log writing when overriding <see cref="EnableAuditLog"/> to <c>true</c> (optional)
    ///
    /// Usage in a consuming project:
    /// <code>
    /// public class AppDbContext : BaseDbContext
    /// {
    ///     public AppDbContext(DbContextOptions&lt;AppDbContext&gt; options,
    ///         ICurrentUserService currentUser,
    ///         IDomainEventDispatcher domainEventDispatcher = null)
    ///         : base(options, currentUser, domainEventDispatcher) { }
    ///
    ///     public DbSet&lt;Order&gt; Orders { get; set; }
    ///
    ///     // Optional: enable audit log table
    ///     protected override bool EnableAuditLog =&gt; true;
    ///     public DbSet&lt;AuditLog&gt; AuditLogs { get; set; }
    ///
    ///     protected override void OnModelCreating(ModelBuilder modelBuilder)
    ///     {
    ///         base.OnModelCreating(modelBuilder);
    ///         modelBuilder.ApplyGlobalFiltersSoftDeleted();
    ///         modelBuilder.ApplyGlobalFiltersTenanted(TenantId);   // optional
    ///         modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class BaseDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDomainEventDispatcher _domainEventDispatcher;

        /// <summary>
        /// Constructor with domain event dispatching support.
        /// Inject <see cref="IDomainEventDispatcher"/> from DI after calling
        /// <c>services.AddDomainEvents(Assembly)</c>.
        /// </summary>
        protected BaseDbContext(
            DbContextOptions options,
            ICurrentUserService currentUserService,
            IDomainEventDispatcher domainEventDispatcher)
            : base(options)
        {
            _currentUserService = currentUserService;
            _domainEventDispatcher = domainEventDispatcher;
        }

        /// <summary>Standard constructor (no domain event dispatching).</summary>
        protected BaseDbContext(DbContextOptions options, ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        /// <summary>Parameterless ctor for EF design-time tools (migrations).</summary>
        protected BaseDbContext(DbContextOptions options) : base(options) { }

        // ----------------------------------------------------------------
        // Optional overrides
        // ----------------------------------------------------------------

        /// <summary>
        /// Override to <c>true</c> in your DbContext to enable automatic audit log writing.
        /// Also add <c>DbSet&lt;AuditLog&gt; AuditLogs { get; set; }</c> to your context.
        /// </summary>
        protected virtual bool EnableAuditLog => false;

        // ----------------------------------------------------------------
        // SaveChanges overrides
        // ----------------------------------------------------------------

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Collect audit entries before state changes (while OldValues are accessible)
            var auditEntries = EnableAuditLog ? BuildAuditEntries() : null;

            SetAuditAndSoftDeleteFields();

            // Collect domain events before clearing them
            var domainEvents = ChangeTracker.Entries<BaseEntity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            // Clear domain events to prevent re-dispatch on double-save
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
                entry.Entity.ClearDomainEvents();

            // Persist audit logs alongside the main save
            if (auditEntries != null && auditEntries.Count > 0)
            {
                foreach (var log in auditEntries)
                    Set<AuditLog>().Add(log);
            }

            var result = await base.SaveChangesAsync(cancellationToken);

            // Dispatch domain events after successful commit
            if (_domainEventDispatcher != null && domainEvents.Count > 0)
                await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

            return result;
        }

        public override int SaveChanges()
        {
            SetAuditAndSoftDeleteFields();

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
                entry.Entity.ClearDomainEvents();

            return base.SaveChanges();
        }

        // ----------------------------------------------------------------
        // Private helpers
        // ----------------------------------------------------------------

        private void SetAuditAndSoftDeleteFields()
        {
            var userId = _currentUserService?.UserId ?? "system";
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (entry.Entity.CreatedAt == default)
                            entry.Entity.CreatedAt = now;
                        if (string.IsNullOrEmpty(entry.Entity.CreatedBy))
                            entry.Entity.CreatedBy = userId;
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = userId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = userId;
                        break;

                    case EntityState.Deleted when entry.Entity is ISoftDelete softDelete:
                        // Intercept hard-delete → convert to soft-delete
                        entry.State = EntityState.Modified;
                        softDelete.DeletedAt = now;
                        softDelete.DeletedBy = userId;
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = userId;
                        break;
                }
            }
        }

        private List<AuditLog> BuildAuditEntries()
        {
            var userId = _currentUserService?.UserId ?? "system";
            var now = DateTime.UtcNow;
            var logs = new List<AuditLog>();

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var log = new AuditLog
                {
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    EntityId = entry.Entity.Id.ToString(),
                    ChangedBy = userId,
                    ChangedAt = now
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        log.Action = "Create";
                        log.NewValues = JsonSerializer.Serialize(
                            entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                        break;

                    case EntityState.Modified:
                        log.Action = "Update";
                        var changed = entry.Properties.Where(p => p.IsModified).ToList();
                        log.ChangedColumns = string.Join(",", changed.Select(p => p.Metadata.Name));
                        log.OldValues = JsonSerializer.Serialize(
                            changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                        log.NewValues = JsonSerializer.Serialize(
                            changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                        break;

                    case EntityState.Deleted:
                        log.Action = "Delete";
                        log.OldValues = JsonSerializer.Serialize(
                            entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                        break;
                }

                logs.Add(log);
            }

            return logs;
        }
    }
}
