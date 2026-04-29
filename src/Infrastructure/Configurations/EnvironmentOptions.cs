using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace MonkoraEdge.Core.Auth.Infrastructure.Configurations;

[ExcludeFromCodeCoverage]
public class EnvironmentOptions
{
    [Required] public string POSTGRES_CONNECTIONSTRING { get; init; }

    // Basic auth credentials for internal tooling endpoints (e.g., health with auth).
    // Remove [Required] if BasicAuth middleware is not registered.
    public string? BASIC_AUTHENTICATION_USERNAME { get; init; }
    public string? BASIC_AUTHENTICATION_PASSWORD { get; init; }

    [Required] public int TOKEN_EXPIRES_IN_MINUTES { get; init; }
    [Required] public string AUTH_ISSUER { get; init; }
    [Required] public string OAUTH2_SIGNED_PRIVATE_KEY { get; init; }
    [Required] public string REDIS_CONNECTIONSTRING { get; init; }

    // Static API keys list (comma-separated) — optional if API key auth is not used.
    public string? API_KEYS { get; init; }

    // Google OAuth2/OIDC social login settings — optional at startup, required at runtime if social login is configured.
    public string? GOOGLE_API_AUTH_KEY { get; init; }
    public string? GOOGLE_API_AUTH_ENDPOINT { get; init; }
    public string? GOOGLE_OAUTH2_API_AUTH_ENDPOINT { get; init; }
    public string? GOOGLE_APPLICATION_CREDENTIALS_AUTH { get; init; }

    [Required] public string NOTIFICATION_ENDPOINT { get; init; }
    [Required] public string BP_API_ENDPOINT { get; init; }
    [Required] public int SIGNIN_FAILED_IN_MINUTES { get; init; }
    [Required] public int BLOCK_IP_ADDRESS_IN_MINUTES { get; init; }
    public string? RESOURCE_API_ENDPOINT { get; init; }
    public string? FACEBOOK_API_ENDPOINT { get; init; }
    public string? APPLE_API_ENDPOINT { get; init; }

    public string? AUTH_JWKS_ENDPOINT { get; init; }

    // Legacy external system endpoints — remove when those integrations are removed.
    public string? ACCOUNT_TOYO_ENDPOINT { get; init; }
    public string? CLIENT_TOYO_FORGOT_PASSWORD_ENDPOINT { get; init; }

    public string? ACCOUNT_DELETION_GRACE_PERIOD_DAYS { get; init; }

    // AMQP settings — optional; only required if message broker features are enabled.
    public string? AMQP_HOST { get; set; }
    public string? AMQP_PORT { get; set; }
    public string? AMQP_USERNAME { get; set; }
    public string? AMQP_PASSWORD { get; set; }
    public string? AMQP_SYNC_DELETE_ACCOUNT_EXCHANGE_KEY { get; set; }

    // HMAC/hashing key — used for token hashing. Keep separate from AES key.
    [Required] public string HASH_SECRET_KEY { get; init; }
    public string? HASH_SECRET_IV { get; init; }

    /// <summary>
    /// AES-256 symmetric encryption key (Base64 or raw string).
    /// Used for encrypting provider client secrets and social login access tokens.
    /// MUST be different from <see cref="HASH_SECRET_KEY"/> (key separation principle).
    /// </summary>
    [Required] public string AES_ENCRYPTION_KEY { get; init; }

    /// <summary>
    /// Audience claim (<c>aud</c>) embedded in access tokens. Must match <c>ValidAudience</c>
    /// on every resource server that validates tokens from this AS.
    /// MUST differ from <see cref="AUTH_ISSUER"/> to prevent audience confusion attacks (RFC 8707).
    /// </summary>
    [Required] public string OAUTH2_AUDIENCE { get; init; }
}