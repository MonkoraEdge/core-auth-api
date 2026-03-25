using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class EmailVerification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string VerificationType { get; set; } = "REGISTRATION";

    public string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
