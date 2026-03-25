using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class User : BaseEntity, ISoftDelete
{
    public Guid? TenantId { get; set; }

    public string? DisplayName { get; set; }
    public string? LocaleCode { get; set; }
    public string? Zoneinfo { get; set; }

    public string? PhoneNumber { get; set; }
    public bool PhoneVerified { get; set; }
    public string Email { get; set; }
    public bool EmailVerified { get; set; }

    public string Status { get; set; } = "INACTIVE";
    public string RegistrationSource { get; set; } = "LOCAL";

    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? LastPasswordChangedAt { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
