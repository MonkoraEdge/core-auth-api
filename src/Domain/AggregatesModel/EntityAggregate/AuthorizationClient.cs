using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AuthorizationClient : BaseEntity, ISoftDelete
{
    public Guid? TenantId { get; set; }

    public string ClientId { get; set; }
    public string? ClientSecretHash { get; set; }
    public string ClientName { get; set; }
    public string ClientType { get; set; } = "CONFIDENTIAL";

    public string TokenEndpointAuthMethod { get; set; } = "CLIENT_SECRET_BASIC";

    public bool RequirePkce { get; set; } = true;
    public string? PkceCodeChallengeMethod { get; set; } = "S256";
    public bool RequireConsent { get; set; } = true;

    public string[] RedirectUris { get; set; } = Array.Empty<string>();
    public string[]? PostLogoutRedirectUris { get; set; }

    public string[]? AllowedGrantTypes { get; set; }
    public string[]? AllowedResponseTypes { get; set; }

    public int AccessTokenLifetime { get; set; } = 3600;
    public int RefreshTokenLifetime { get; set; } = 2592000;

    public string? LogoUri { get; set; }
    public string? ClientUri { get; set; }
    public string? JwksUri { get; set; }
    public string? Jwks { get; set; }

    public DateTime? ClientSecretExpiresAt { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
