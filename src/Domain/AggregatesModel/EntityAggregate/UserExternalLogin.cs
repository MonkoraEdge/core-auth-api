using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class UserExternalLogin : BaseEntity, ISoftDelete
{
    public Guid UserId { get; set; }
    public Guid ProviderId { get; set; }

    public string ProviderUserId { get; set; }
    public string? ProviderDisplayName { get; set; }
    public string? ProviderEmail { get; set; }

    public string? AccessTokenEncrypt { get; set; }
    public string? RefreshTokenEncrypt { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public string[]? Scopes { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
