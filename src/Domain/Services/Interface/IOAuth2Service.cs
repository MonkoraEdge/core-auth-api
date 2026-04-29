using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface IOAuth2Service
{
    /// <summary>Process the authorization endpoint request and decide the HTTP-facing outcome.</summary>
    Task<AuthorizeEndpointResponse> ProcessAuthorizeRequestAsync(AuthorizeRequest request, Guid? authenticatedUserId);

    /// <summary>Process authorization request — validate client, scope, redirect_uri</summary>
    Task<AuthorizeValidationResult> ValidateAuthorizeRequestAsync(AuthorizeRequest request, Guid authenticatedUserId);

    /// <summary>Process a consent submission and return the redirect result.</summary>
    Task<ConsentResponse> ProcessConsentAsync(ConsentRequest request, Guid userId);

    /// <summary>Issue authorization code after user consents</summary>
    Task<string> IssueAuthorizationCodeAsync(AuthorizeRequest request, Guid userId, bool rememberConsent);

    /// <summary>Dispatch token endpoint grant processing with OAuth-compliant validation.</summary>
    Task<TokenResponse> ProcessTokenRequestAsync(TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null);

    /// <summary>Exchange authorization code for tokens</summary>
    Task<TokenResponse> ExchangeAuthorizationCodeAsync(TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null);

    /// <summary>Issue tokens for client_credentials grant</summary>
    Task<TokenResponse> ClientCredentialsGrantAsync(TokenRequest request, string clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null);

    /// <summary>Refresh access token</summary>
    Task<TokenResponse> RefreshTokenGrantAsync(TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null);

    /// <summary>Revoke a token (RFC 7009)</summary>
    Task RevokeAsync(RevocationRequest request, string clientId, string? clientSecret);

    /// <summary>Introspect a token (RFC 7662)</summary>
    Task<IntrospectResponse> IntrospectAsync(IntrospectRequest request, string clientId, string? clientSecret);

    /// <summary>Get userinfo claims for an access token</summary>
    Task<UserInfoResponse> GetUserInfoAsync(string accessToken);

    /// <summary>Build OpenID Connect discovery metadata for the current base URL.</summary>
    OpenIdConfigurationResponse GetOpenIdConfiguration(string baseUrl);

    /// <summary>Build OAuth 2.0 Authorization Server metadata (RFC 8414).</summary>
    AuthorizationServerMetadataResponse GetAuthorizationServerMetadata(string baseUrl);

    /// <summary>
    /// End session / RP-initiated logout — revokes all user tokens and returns the
    /// validated post-logout redirect URI if it matches the client's registered list.
    /// Returns <c>null</c> when no safe redirect target can be confirmed.
    /// </summary>
    Task<string?> EndSessionAsync(Guid? userId, string? idTokenHint, string? postLogoutRedirectUri, string? clientId = null);

    /// <summary>
    /// RFC 8628 §3.1 — Device Authorization Request.
    /// Issues a device_code and user_code pair and returns polling parameters.
    /// </summary>
    Task<DeviceAuthorizationResponse> DeviceAuthorizationAsync(DeviceAuthorizationRequest request, string? clientId, string? clientSecret);

    /// <summary>
    /// RFC 8628 §3.4 — Approve or deny a pending device authorization (called from the user-agent verification flow).
    /// </summary>
    Task ApproveDeviceCodeAsync(string userCode, Guid userId, bool approved);

    /// <summary>
    /// RFC 9126 — Push authorization request parameters, store them in cache, and return a request_uri.
    /// The request_uri can then be passed to the authorize endpoint instead of inline parameters.
    /// </summary>
    Task<PushedAuthorizationResponse> PushAuthorizationRequestAsync(
        PushedAuthorizationFormRequest form, string? clientId, string? clientSecret);

    /// <summary>
    /// RFC 7591 — Dynamic client registration. Creates a new OAuth client from the supplied metadata
    /// and returns the assigned credentials.
    /// </summary>
    Task<DynamicClientRegistrationResponse> RegisterClientDynamicallyAsync(DynamicClientRegistrationRequest request);

    /// <summary>
    /// RFC 8693 — Token Exchange grant. Validates the subject_token and issues a new access token
    /// (optionally scoped to a specific audience) with an embedded act claim.
    /// </summary>
    Task<TokenResponse> TokenExchangeGrantAsync(TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null);
}
