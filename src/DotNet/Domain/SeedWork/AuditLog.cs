namespace MonkoraEdge.Core.DotNet.Domain.SeedWork
{
    /// <summary>
    /// Stores a record of every Create / Update / Delete operation on an entity.
    /// Add <c>DbSet&lt;AuditLog&gt; AuditLogs</c> to your DbContext and call
    /// <c>modelBuilder.ApplyConfigurationsFromAssembly(...)</c> or configure manually.
    ///
    /// <c>BaseDbContext</c> automatically writes audit log entries when
    /// <c>EnableAuditLog</c> is overridden to <c>true</c> in your DbContext subclass.
    ///
    /// Old/new values are serialized as JSON using <c>System.Text.Json</c>.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Name of the database table / entity type being audited.</summary>
        public string TableName { get; set; }

        /// <summary>Primary key of the audited entity.</summary>
        public string EntityId { get; set; }

        /// <summary>"Create", "Update", or "Delete".</summary>
        public string Action { get; set; }

        /// <summary>JSON snapshot of the entity before the change. Null for Create actions.</summary>
        public string OldValues { get; set; }

        /// <summary>JSON snapshot of the entity after the change. Null for Delete actions.</summary>
        public string NewValues { get; set; }

        /// <summary>Names of properties that changed (comma-separated). Null for Create/Delete.</summary>
        public string ChangedColumns { get; set; }

        /// <summary>User who performed the operation (from ICurrentUserService).</summary>
        public string ChangedBy { get; set; }

        /// <summary>UTC timestamp of the operation.</summary>
        public DateTime ChangedAt { get; set; }
    }
}
