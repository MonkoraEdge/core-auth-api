using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class Permission : BaseEntity, ISoftDelete
{
    public Guid? TenantId { get; set; }
    public string PermissionCode { get; set; }
    public Locale PermissionName { get; set; }
    public string? Resource { get; set; }
    public string? Action { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>Soft-deletes the permission and deactivates it so it can no longer be granted.</summary>
    public void SoftDelete(string? deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }
}
