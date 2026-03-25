using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface IOAuth2Service
{
    /// <summary>Process authorization request — validate client, scope, redirect_uri</summary>
    Task<AuthorizeValidationResult> ValidateAuthorizeRequestAsync(AuthorizeRequest request, Guid authenticatedUserId);

    /// <summary>Issue authorization code after user consents</summary>
    Task<string> IssueAuthorizationCodeAsync(AuthorizeRequest request, Guid userId, bool rememberConsent);

    /// <summary>Exchange authorization code for tokens</summary>
    Task<TokenResponse> ExchangeAuthorizationCodeAsync(TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent);

    /// <summary>Issue tokens for client_credentials grant</summary>
    Task<TokenResponse> ClientCredentialsGrantAsync(TokenRequest request, string clientId, string? clientSecret, string? ipAddress, string? userAgent);

    /// <summary>Refresh access token</summary>
    Task<TokenResponse> RefreshTokenGrantAsync(TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent);

    /// <summary>Revoke a token (RFC 7009)</summary>
    Task RevokeAsync(RevocationRequest request, string clientId, string? clientSecret);

    /// <summary>Introspect a token (RFC 7662)</summary>
    Task<IntrospectResponse> IntrospectAsync(IntrospectRequest request, string clientId, string? clientSecret);

    /// <summary>Get userinfo claims for an access token</summary>
    Task<UserInfoResponse> GetUserInfoAsync(string accessToken);

    /// <summary>End session / RP-initiated logout — revokes all user tokens (RFC 8414)</summary>
    Task EndSessionAsync(Guid userId, string? idTokenHint);
}
