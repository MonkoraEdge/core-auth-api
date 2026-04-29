using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class Scope : BaseEntity, ISoftDelete
{
    public string ScopeName { get; set; }
    public string? DisplayName { get; set; }
    public string ScopeType { get; set; } = "CUSTOM";
    public string[]? Claims { get; set; }

    public bool IsSystemScope { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>Soft-deletes the scope and deactivates it so it can no longer be requested.</summary>
    public void SoftDelete(string? deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }
}
