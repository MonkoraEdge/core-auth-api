using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.Auth.Domain.Services;

public class OAuth2Service : IOAuth2Service
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationCodeRepository _authCodeRepo;
    private readonly IAuthorizationConsentRepository _consentRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private readonly IClientAuthenticator _clientAuth;
    private readonly IPasswordService _passwordService;

    public OAuth2Service(
        IUnitOfWork unitOfWork,
        IAuthorizationCodeRepository authCodeRepo,
        IAuthorizationConsentRepository consentRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUserRepository userRepo,
        ITokenService tokenService,
        IClientAuthenticator clientAuth,
        IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _authCodeRepo = authCodeRepo;
        _consentRepo = consentRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _userRepo = userRepo;
        _tokenService = tokenService;
        _clientAuth = clientAuth;
        _passwordService = passwordService;
    }

    public async Task<AuthorizeValidationResult> ValidateAuthorizeRequestAsync(AuthorizeRequest request, Guid authenticatedUserId)
    {
        if (string.IsNullOrEmpty(request.ClientId))
            return AuthorizeValidationResult.Fail("invalid_request", "client_id is required.");

        if (string.IsNullOrEmpty(request.ResponseType))
            return AuthorizeValidationResult.Fail("invalid_request", "response_type is required.");

        if (request.ResponseType != "code")
            return AuthorizeValidationResult.Fail("unsupported_response_type", "Only 'code' response_type is supported.");

        var client = await _clientAuth.LoadAsync(request.ClientId);

        if (string.IsNullOrEmpty(request.RedirectUri))
            return AuthorizeValidationResult.Fail("invalid_request", "redirect_uri is required.");

        if (!client.RedirectUris.Any(r => string.Equals(r, request.RedirectUri, StringComparison.Ordinal)))
            return AuthorizeValidationResult.Fail("invalid_request", "redirect_uri does not exactly match a registered URI.");

        if (string.IsNullOrWhiteSpace(request.State))
            return AuthorizeValidationResult.Fail("invalid_request", "state parameter is required.");

        bool requiresPkce = client.ClientType == "PUBLIC" || client.RequirePkce;
        if (requiresPkce)
        {
            if (string.IsNullOrEmpty(request.CodeChallenge))
                return AuthorizeValidationResult.Fail("invalid_request", "code_challenge is required.");
            if (request.CodeChallengeMethod != "S256")
                return AuthorizeValidationResult.Fail("invalid_request", "code_challenge_method must be S256.");
        }

        var requestedScopes = (request.Scope ?? "openid")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var allowedScopes = await _clientAuth.GetAllowedScopeNamesAsync(client.Id);

        var invalidScopes = requestedScopes.Except(allowedScopes).ToArray();
        if (invalidScopes.Any())
            return AuthorizeValidationResult.Fail("invalid_scope", $"Scope(s) not allowed: {string.Join(", ", invalidScopes)}");

        var allowedGrants = client.AllowedGrantTypes ?? Array.Empty<string>();
        if (!allowedGrants.Contains("authorization_code", StringComparer.OrdinalIgnoreCase))
            return AuthorizeValidationResult.Fail("unauthorized_client", "Client is not authorized for authorization_code grant.");

        bool requiresConsent = client.RequireConsent;
        if (requiresConsent && authenticatedUserId != Guid.Empty)
        {
            var existingConsent = await _consentRepo.GetActiveByUserAndClientAsync(authenticatedUserId, client.Id);
            if (existingConsent != null && requestedScopes.All(s => existingConsent.Scopes.Contains(s)))
                requiresConsent = false;
        }

        return new AuthorizeValidationResult
        {
            IsValid = true,
            RequiresConsent = requiresConsent,
            RequiresLogin = authenticatedUserId == Guid.Empty,
            Client = new AuthorizationClientInfo
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                LogoUri = client.LogoUri,
                ClientUri = client.ClientUri
            },
            RequestedScopes = requestedScopes
        };
    }

    public async Task<string> IssueAuthorizationCodeAsync(AuthorizeRequest request, Guid userId, bool rememberConsent)
    {
        var client = await _clientAuth.LoadAsync(request.ClientId);
        var scopes = (request.Scope ?? "openid").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (rememberConsent)
        {
            var existingConsent = await _consentRepo.GetActiveByUserAndClientAsync(userId, client.Id);
            if (existingConsent != null)
            {
                existingConsent.Scopes = existingConsent.Scopes.Union(scopes).ToArray();
                _consentRepo.Update(existingConsent);
            }
            else
            {
                _consentRepo.Insert(new AuthorizationConsent
                {
                    ClientId = client.Id,
                    UserId = userId,
                    Scopes = scopes,
                    GrantedAt = DateTime.UtcNow
                });
            }
        }

        // GenerateAuthorizationCodeAsync inserts but does NOT save — consent + code
        // land in one atomic commit below
        var code = await _tokenService.GenerateAuthorizationCodeAsync(
            client.Id, userId, null, scopes, request.RedirectUri!,
            request.CodeChallenge, request.CodeChallengeMethod, request.Nonce);

        await _unitOfWork.SaveChangesAsync();
        return code;
    }

    public async Task<TokenResponse> ExchangeAuthorizationCodeAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent)
    {
        var client = await _clientAuth.AuthenticateAsync(clientId ?? request.ClientId, clientSecret);

        if (string.IsNullOrEmpty(request.Code))
            throw new CustomHttpBadRequestException("token", "code is required.");

        if (string.IsNullOrEmpty(request.RedirectUri))
            throw new CustomHttpBadRequestException("token", "redirect_uri is required.");

        var codeHash = _tokenService.HashToken(request.Code);
        var authCode = await _authCodeRepo.GetByCodeHashAsync(codeHash);

        if (authCode == null || authCode.ConsumedAt.HasValue || authCode.ExpiresAt <= DateTime.UtcNow)
            throw new CustomHttpBadRequestException("token", "Authorization code is invalid, expired, or already used.");

        if (authCode.ClientId != client.Id)
            throw new CustomHttpBadRequestException("token", "Code was not issued to this client.");

        if (!string.Equals(authCode.RedirectUri, request.RedirectUri, StringComparison.OrdinalIgnoreCase))
            throw new CustomHttpBadRequestException("token", "redirect_uri mismatch.");

        if (!string.IsNullOrEmpty(authCode.CodeChallenge))
        {
            if (string.IsNullOrEmpty(request.CodeVerifier))
                throw new CustomHttpBadRequestException("token", "code_verifier is required.");

            if (!_passwordService.VerifyPkceCodeVerifier(request.CodeVerifier, authCode.CodeChallenge, authCode.CodeChallengeMethod ?? "S256"))
                throw new CustomHttpBadRequestException("token", "code_verifier is invalid.");
        }

        // Mark code consumed; generate tokens; commit all three changes in one transaction
        authCode.ConsumedAt = DateTime.UtcNow;
        _authCodeRepo.Update(authCode);

        var scopes = authCode.Scopes;
        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, authCode.UserId, scopes, "authorization_code", ipAddress, userAgent);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(
            Guid.Empty, client.Id, authCode.UserId, authCode.SessionId, scopes,
            client.RefreshTokenLifetime, familyId: Guid.NewGuid());

        string? idToken = null;
        if (scopes.Contains("openid"))
            idToken = await _tokenService.GenerateIdTokenAsync(
                client.Id, authCode.UserId, scopes,
                authCode.Nonce, authCode.AuthTime ?? DateTime.UtcNow);

        await _unitOfWork.SaveChangesAsync(); // single atomic commit

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = client.AccessTokenLifetime,
            RefreshToken = refreshToken,
            IdToken = idToken,
            Scope = string.Join(" ", scopes)
        };
    }

    public async Task<TokenResponse> ClientCredentialsGrantAsync(
        TokenRequest request, string clientId, string? clientSecret, string? ipAddress, string? userAgent)
    {
        var client = await _clientAuth.AuthenticateAsync(clientId, clientSecret);

        var allowedGrants = client.AllowedGrantTypes ?? Array.Empty<string>();
        if (!allowedGrants.Contains("client_credentials", StringComparer.OrdinalIgnoreCase))
            throw new CustomHttpBadRequestException("token", "Client is not authorized for client_credentials grant.");

        var requestedScopes = (request.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var allowedScopes = await _clientAuth.GetAllowedScopeNamesAsync(client.Id);

        var invalidScopes = requestedScopes.Except(allowedScopes).ToArray();
        if (invalidScopes.Any())
            throw new CustomHttpBadRequestException("token", $"Scope(s) not allowed: {string.Join(", ", invalidScopes)}");

        var finalScopes = requestedScopes.Any() ? requestedScopes : allowedScopes.ToArray();
        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, null, finalScopes, "client_credentials", ipAddress, userAgent);

        await _unitOfWork.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = client.AccessTokenLifetime,
            Scope = string.Join(" ", finalScopes)
        };
    }

    public async Task<TokenResponse> RefreshTokenGrantAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            throw new CustomHttpBadRequestException("token", "refresh_token is required.");

        var client = await _clientAuth.AuthenticateAsync(clientId ?? request.ClientId, clientSecret);

        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var rt = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);

        if (rt == null)
            throw new CustomHttpBadRequestException("token", "The refresh token is invalid.");

        // Theft detection: a revoked token being re-presented means the rotation chain
        // is compromised. Revoke the entire family before rejecting.
        if (rt.RevokedAt.HasValue)
        {
            if (rt.FamilyId != Guid.Empty)
                await _tokenService.RevokeTokenFamilyAsync(rt.FamilyId, "refresh_token_reuse_detected");

            throw new CustomHttpBadRequestException("token",
                "The refresh token has already been used. All sessions in this chain have been revoked for security.");
        }

        if (rt.ExpiresAt <= DateTime.UtcNow)
            throw new CustomHttpBadRequestException("token", "The refresh token has expired.");

        if (rt.ClientId != client.Id)
            throw new CustomHttpBadRequestException("token", "Refresh token was not issued to this client.");

        var scopes = rt.Scopes;
        var userId = rt.UserId == Guid.Empty ? (Guid?)null : rt.UserId;

        // Revoke old token, issue new pair, commit atomically
        rt.RevokedAt = DateTime.UtcNow;
        _refreshTokenRepo.Update(rt);

        var newAccessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, userId, scopes, "refresh_token", ipAddress, userAgent);
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(
            Guid.Empty, client.Id, userId, rt.SessionId, scopes,
            client.RefreshTokenLifetime, familyId: rt.FamilyId);

        await _unitOfWork.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            TokenType = "Bearer",
            ExpiresIn = client.AccessTokenLifetime,
            RefreshToken = newRefreshToken,
            Scope = string.Join(" ", scopes)
        };
    }

    public async Task RevokeAsync(RevocationRequest request, string clientId, string? clientSecret)
    {
        await _clientAuth.AuthenticateAsync(clientId, clientSecret);

        if (string.IsNullOrEmpty(request.Token))
            return; // RFC 7009: servers should not return an error for missing token

        var client = await _clientAuth.LoadAsync(clientId);
        await _tokenService.RevokeTokenAsync(request.Token, request.TokenTypeHint, client.Id, "revoked_by_client");
    }

    public async Task<IntrospectResponse> IntrospectAsync(IntrospectRequest request, string clientId, string? clientSecret)
    {
        await _clientAuth.AuthenticateAsync(clientId, clientSecret);
        return await _tokenService.IntrospectTokenAsync(request.Token, request.TokenTypeHint);
    }

    public async Task<UserInfoResponse> GetUserInfoAsync(string accessToken)
    {
        var introspect = await _tokenService.IntrospectTokenAsync(accessToken, "access_token");
        if (!introspect.Active || string.IsNullOrEmpty(introspect.Sub))
            throw new CustomHttpBadRequestException("userinfo", "Invalid or expired access token.");

        if (!Guid.TryParse(introspect.Sub, out var userId))
            throw new CustomHttpBadRequestException("userinfo", "Invalid subject claim.");

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new CustomHttpBadRequestException("userinfo", "User not found.");

        return new UserInfoResponse
        {
            Sub = user.Id.ToString(),
            Name = user.DisplayName,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            PhoneNumber = user.PhoneNumber,
            Locale = user.LocaleCode,
            Zoneinfo = user.Zoneinfo
        };
    }

    public async Task EndSessionAsync(Guid userId, string? idTokenHint)
    {
        // Revoke all active access + refresh tokens for this user
        await _tokenService.RevokeAllUserTokensAsync(userId, sessionId: null, reason: "end_session");
    }

    // ─── Private helpers ─────────────────────────────────────────────────────────
    // (All client auth and scope logic has moved to ClientAuthenticator)
}
