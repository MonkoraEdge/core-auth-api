using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate;
using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class AuthorizationClient : BaseEntity, ISoftDelete, IAggregateRoot
{
    public Guid? TenantId { get; set; }

    public string ClientId { get; set; }
    public string? ClientSecretHash { get; set; }
    public string ClientName { get; set; }
    public string ClientType { get; set; } = OAuthClientType.Confidential;

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

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>True when this is a public client — PKCE-only, no client secret (RFC 6749 §2.1).</summary>
    public bool IsPublic
        => string.Equals(ClientType, OAuthClientType.Public, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns true when the client secret has passed its registered expiry date.</summary>
    public bool IsSecretExpired()
        => ClientSecretExpiresAt.HasValue && ClientSecretExpiresAt.Value < DateTime.UtcNow;

    /// <summary>Returns true when <paramref name="uri"/> exactly matches one of the registered redirect URIs.</summary>
    public bool IsRedirectUriRegistered(string uri)
        => RedirectUris.Any(r => string.Equals(r, uri, StringComparison.Ordinal));

    /// <summary>Returns true when <paramref name="grantType"/> appears in the client's allowed-grant list.</summary>
    public bool IsGrantTypeAllowed(string grantType)
        => AllowedGrantTypes?.Contains(grantType, StringComparer.OrdinalIgnoreCase) == true;
}
