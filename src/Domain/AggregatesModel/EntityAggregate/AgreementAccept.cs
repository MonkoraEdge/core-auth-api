using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AgreementAccept : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AgreementId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? SessionId { get; set; }

    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public DateTime? WithdrawnAt { get; set; }
    public string? WithdrawnReason { get; set; }

    public string AcceptanceMethod { get; set; } = "CHECKBOX";

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool IsActive { get; set; } = true;
}
