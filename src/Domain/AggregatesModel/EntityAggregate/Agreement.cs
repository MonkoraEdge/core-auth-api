using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class Agreement : BaseEntity, ISoftDelete
{
    public Guid? TenantId { get; set; }

    public string AgreementCode { get; set; }
    public string AgreementType { get; set; }

    public Locale Title { get; set; }
    public Locale? Content { get; set; }
    public Locale? Summary { get; set; }

    public string Version { get; set; }
    public DateTime EffectiveAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool IsRequired { get; set; } = true;
    public bool RequiresExplicitAction { get; set; } = true;

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>Soft-deletes the agreement so it is no longer presented to users.</summary>
    public void SoftDelete(string? deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }
}
