using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class UserIdentity : BaseEntity, ISoftDelete
{
    public Guid UserId { get; set; }

    public string ProviderType { get; set; } = "LOCAL";
    public string Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? PasswordAlgo { get; set; }
    public DateTime? PasswordUpdatedAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
