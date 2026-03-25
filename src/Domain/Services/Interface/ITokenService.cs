using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface ITokenService
{
    /// <summary>Generate a signed JWT access token</summary>
    Task<string> GenerateAccessTokenAsync(Guid clientId, Guid? userId, string[] scopes, string? grantType, string? ipAddress, string? userAgent);

    /// <summary>Generate an opaque refresh token string and persist it</summary>
    Task<string> GenerateRefreshTokenAsync(Guid accessTokenId, Guid clientId, Guid? userId, Guid? sessionId, string[] scopes, int lifetimeSeconds, Guid? familyId = null);

    /// <summary>Generate a signed OIDC ID token — audience is the client_id, nonce prevents replay</summary>
    Task<string> GenerateIdTokenAsync(Guid clientId, Guid userId, string[] scopes, string? nonce, DateTime authTime);

    /// <summary>Generate a short-lived authorization code with PKCE</summary>
    Task<string> GenerateAuthorizationCodeAsync(Guid clientId, Guid userId, Guid? sessionId, string[] scopes, string redirectUri, string? codeChallenge, string? codeChallengeMethod, string? nonce);

    /// <summary>Validate and introspect a token — returns null if invalid/revoked</summary>
    Task<IntrospectResponse> IntrospectTokenAsync(string token, string? tokenTypeHint);

    /// <summary>Revoke an access or refresh token</summary>
    Task RevokeTokenAsync(string token, string? tokenTypeHint, Guid clientId, string? reason = "revoked");

    /// <summary>Revoke all tokens for a user session</summary>
    Task RevokeAllUserTokensAsync(Guid userId, Guid? sessionId = null, string reason = "logout");

    /// <summary>Revoke all refresh tokens in a family — called when token reuse/theft is detected</summary>
    Task RevokeTokenFamilyAsync(Guid familyId, string reason = "refresh_token_reuse");

    /// <summary>Hash a token for secure storage</summary>
    string HashToken(string token);

    /// <summary>Get JWKS public keys for token verification</summary>
    JwksResponse GetJwks();

    /// <summary>Get OpenID Connect issuer URL</summary>
    string GetIssuer();
}
