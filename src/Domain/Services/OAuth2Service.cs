using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;

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
    private readonly IRefreshTokenProcessor _refreshTokenProcessor;

    public OAuth2Service(
        IUnitOfWork unitOfWork,
        IAuthorizationCodeRepository authCodeRepo,
        IAuthorizationConsentRepository consentRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUserRepository userRepo,
        ITokenService tokenService,
        IClientAuthenticator clientAuth,
        IPasswordService passwordService,
        IRefreshTokenProcessor refreshTokenProcessor)
    {
        _unitOfWork = unitOfWork;
        _authCodeRepo = authCodeRepo;
        _consentRepo = consentRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _userRepo = userRepo;
        _tokenService = tokenService;
        _clientAuth = clientAuth;
        _passwordService = passwordService;
        _refreshTokenProcessor = refreshTokenProcessor;
    }

    public async Task<AuthorizeEndpointResponse> ProcessAuthorizeRequestAsync(AuthorizeRequest request, Guid? authenticatedUserId)
    {
        var validation = await ValidateAuthorizeRequestAsync(request, authenticatedUserId ?? Guid.Empty);
        if (!validation.IsValid)
        {
            if (!string.IsNullOrEmpty(request.RedirectUri))
            {
                return new AuthorizeEndpointResponse
                {
                    Kind = AuthorizeResponseKind.Redirect,
                    RedirectUrl = BuildErrorRedirectUrl(request.RedirectUri, validation.Error ?? "invalid_request",
                        validation.ErrorDescription, request.State)
                };
            }

            return new AuthorizeEndpointResponse
            {
                Kind = AuthorizeResponseKind.Error,
                Error = validation.Error,
                ErrorDescription = validation.ErrorDescription
            };
        }

        if (validation.RequiresLogin)
        {
            return new AuthorizeEndpointResponse
            {
                Kind = AuthorizeResponseKind.LoginRequired,
                Client = validation.Client,
                RequestedScopes = validation.RequestedScopes
            };
        }

        if (validation.RequiresConsent)
        {
            return new AuthorizeEndpointResponse
            {
                Kind = AuthorizeResponseKind.ConsentRequired,
                Client = validation.Client,
                RequestedScopes = validation.RequestedScopes
            };
        }

        var code = await IssueAuthorizationCodeAsync(request, authenticatedUserId!.Value, false);
        return new AuthorizeEndpointResponse
        {
            Kind = AuthorizeResponseKind.Redirect,
            RedirectUrl = BuildCodeRedirectUrl(request.RedirectUri!, code, request.State)
        };
    }

    public async Task<AuthorizeValidationResult> ValidateAuthorizeRequestAsync(AuthorizeRequest request, Guid authenticatedUserId)
    {
        if (string.IsNullOrEmpty(request.ClientId))
            return AuthorizeValidationResult.Fail("invalid_request", "client_id is required.");

        if (string.IsNullOrEmpty(request.ResponseType))
            return AuthorizeValidationResult.Fail("invalid_request", "response_type is required.");

        // OAuth 2.1 §4.1.2: Only 'code' is supported. Implicit ('token') and hybrid ('code token')
        // flows are explicitly prohibited. Return error BEFORE validating redirect_uri to prevent
        // open-redirect abuse via attacker-controlled redirect_uri.
        if (request.ResponseType.Split(' ').Any(t => t.Equals("token", StringComparison.OrdinalIgnoreCase)))
            return AuthorizeValidationResult.Fail("unsupported_response_type",
                "Implicit grant and hybrid flows are not supported. Use response_type=code with PKCE.");

        if (request.ResponseType != "code")
            return AuthorizeValidationResult.Fail("unsupported_response_type", "Only response_type=code is supported.");

        var client = await _clientAuth.LoadAsync(request.ClientId);

        if (string.IsNullOrEmpty(request.RedirectUri))
            return AuthorizeValidationResult.Fail("invalid_request", "redirect_uri is required.");

        if (!client.RedirectUris.Any(r => string.Equals(r, request.RedirectUri, StringComparison.Ordinal)))
            return AuthorizeValidationResult.Fail("invalid_request", "redirect_uri does not exactly match a registered URI.");

        if (string.IsNullOrEmpty(request.CodeChallenge))
        {
            return AuthorizeValidationResult.Fail("invalid_request", "code_challenge is required.");
        }

        if (request.CodeChallengeMethod != "S256")
            return AuthorizeValidationResult.Fail("invalid_request", "code_challenge_method must be S256.");

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

        // ── OIDC Core §3.1.2.1: prompt parameter ────────────────────────────────
        // Must be processed after consent state is determined so prompt=none can
        // return the correct error (login_required vs consent_required).
        bool forceLogin = false;
        bool forceConsent = false;
        var prompt = request.Prompt?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(prompt))
        {
            switch (prompt)
            {
                case "none":
                    // prompt=none: server MUST NOT display any interactive UI.
                    // Return error codes from OIDC Core §3.1.2.6 instead of requiring interaction.
                    if (authenticatedUserId == Guid.Empty)
                        return AuthorizeValidationResult.Fail("login_required",
                            "Authentication is required. Re-authenticate and retry.");
                    if (requiresConsent)
                        return AuthorizeValidationResult.Fail("consent_required",
                            "Consent has not been granted for the requested scopes.");
                    break;
                case "login":
                case "select_account":
                    // Force the login flow even when a session already exists.
                    forceLogin = true;
                    break;
                case "consent":
                    // Force the consent screen even when prior consent covers the scopes.
                    forceConsent = true;
                    break;
            }
        }

        // ── OIDC Core §3.1.2.1: max_age parameter ───────────────────────────────
        // If the elapsed time since last authentication exceeds max_age, the user
        // MUST be re-authenticated (even if a valid session exists).
        if (!string.IsNullOrEmpty(request.MaxAge) && authenticatedUserId != Guid.Empty
            && int.TryParse(request.MaxAge, out var maxAgeSeconds) && maxAgeSeconds >= 0)
        {
            var user = await _userRepo.GetByIdAsync(authenticatedUserId);
            var lastAuthAt = user?.LastLoginAt;
            if (!lastAuthAt.HasValue || (DateTime.UtcNow - lastAuthAt.Value).TotalSeconds > maxAgeSeconds)
                forceLogin = true;
        }

        return new AuthorizeValidationResult
        {
            IsValid = true,
            RequiresConsent = requiresConsent || forceConsent,
            RequiresLogin = authenticatedUserId == Guid.Empty || forceLogin,
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

    public async Task<ConsentResponse> ProcessConsentAsync(ConsentRequest request, Guid userId)
    {
        var authorizeReq = new AuthorizeRequest
        {
            ClientId = request.ClientId,
            ResponseType = "code",
            RedirectUri = request.RedirectUri,
            Scope = request.Scope,
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Nonce = request.Nonce
        };

        var validation = await ValidateAuthorizeRequestAsync(authorizeReq, userId);
        if (!validation.IsValid)
        {
            return new ConsentResponse
            {
                RedirectUrl = BuildErrorRedirectUrl(request.RedirectUri, validation.Error ?? "invalid_request",
                    validation.ErrorDescription, request.State)
            };
        }

        if (!request.Approved)
        {
            return new ConsentResponse
            {
                RedirectUrl = BuildErrorRedirectUrl(request.RedirectUri, "access_denied",
                    "User denied access", request.State)
            };
        }

        var code = await IssueAuthorizationCodeAsync(authorizeReq, userId, request.RememberConsent);
        return new ConsentResponse
        {
            RedirectUrl = BuildCodeRedirectUrl(request.RedirectUri, code, request.State)
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

    public async Task<TokenResponse> ProcessTokenRequestAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent)
    {
        var normalizedGrantType = request.GrantType?.Trim().ToLowerInvariant();
        return normalizedGrantType switch
        {
            "authorization_code" => await ExchangeAuthorizationCodeAsync(request, clientId, clientSecret, ipAddress, userAgent),
            "client_credentials" => await ClientCredentialsGrantAsync(request, clientId!, clientSecret, ipAddress, userAgent),
            "refresh_token"      => await RefreshTokenGrantAsync(request, clientId, clientSecret, ipAddress, userAgent),
            // OAuth 2.1 explicitly removes password and implicit grants.
            "password" => throw new DomainException("token", ErrorCodeType.UNSUPPORTED_GRANT_TYPE,
                "The 'password' grant has been removed in OAuth 2.1. Use 'authorization_code' with PKCE."),
            "urn:ietf:params:oauth:grant-type:device_code" => throw new DomainException("token", ErrorCodeType.UNSUPPORTED_GRANT_TYPE,
                "Device code grant is not supported by this server."),
            _ => throw new DomainException("token", ErrorCodeType.UNSUPPORTED_GRANT_TYPE)
        };
    }

    public async Task<TokenResponse> ExchangeAuthorizationCodeAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent)
    {
        var client = await _clientAuth.AuthenticateAsync(clientId ?? request.ClientId, clientSecret);

        if (string.IsNullOrEmpty(request.Code))
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "code is required.");

        if (string.IsNullOrEmpty(request.RedirectUri))
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "redirect_uri is required.");

        var codeHash = _tokenService.HashToken(request.Code);
        var authCode = await _authCodeRepo.GetByCodeHashAsync(codeHash);

        if (authCode == null || authCode.ConsumedAt.HasValue || authCode.ExpiresAt <= DateTime.UtcNow)
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "Authorization code is invalid, expired, or already used.");

        if (authCode.ClientId != client.Id)
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "Code was not issued to this client.");

        if (!string.Equals(authCode.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "redirect_uri mismatch.");

        if (!string.IsNullOrEmpty(authCode.CodeChallenge))
        {
            if (string.IsNullOrEmpty(request.CodeVerifier))
                throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "code_verifier is required.");

            if (!_passwordService.VerifyPkceCodeVerifier(request.CodeVerifier, authCode.CodeChallenge, authCode.CodeChallengeMethod ?? "S256"))
                throw new DomainException("token", ErrorCodeType.INVALID_PKCE_CODE_VERIFIER, "code_verifier is invalid.");
        }

        // Ensure one-time code use under concurrency.
        var consumed = await _authCodeRepo.TryConsumeAsync(authCode.Id, DateTime.UtcNow);
        if (!consumed)
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "Authorization code is invalid, expired, or already used.");

        var scopes = authCode.Scopes;
        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(client.AccessTokenLifetime);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, authCode.UserId, scopes, "authorization_code", ipAddress, userAgent,
            expiresIn);
        string? refreshToken = null;
        if (scopes.Contains("offline_access", StringComparer.Ordinal))
        {
            (refreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(
                client.Id, authCode.UserId, authCode.SessionId, scopes,
                client.RefreshTokenLifetime, familyId: Guid.NewGuid(),
                ipAddress: ipAddress, userAgent: userAgent);
        }

        string? idToken = null;
        if (scopes.Contains("openid"))
            idToken = await _tokenService.GenerateIdTokenAsync(
                client.ClientId, authCode.UserId, scopes,
                authCode.Nonce, authCode.AuthTime ?? DateTime.UtcNow,
                accessToken: accessToken);  // OIDC Core §3.1.3.6: at_hash requires the access token

        await _unitOfWork.SaveChangesAsync(); // single atomic commit

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
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
            throw new DomainException("token", ErrorCodeType.UNAUTHORIZED_CLIENT, "Client is not authorized for client_credentials grant.");

        var requestedScopes = (request.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!requestedScopes.Any())
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "scope is required for client_credentials grant.");

        // RFC 6749 §4.4 / OAuth 2.1 §4.4.3: MUST NOT issue refresh tokens for client_credentials.
        // Reject offline_access explicitly so clients are not misled by a scope that silently does nothing.
        if (requestedScopes.Contains("offline_access", StringComparer.Ordinal))
            throw new DomainException("token", ErrorCodeType.INVALID_SCOPE,
                "offline_access scope is not permitted for the client_credentials grant.");

        var allowedScopes = await _clientAuth.GetAllowedScopeNamesAsync(client.Id);

        var invalidScopes = requestedScopes.Except(allowedScopes).ToArray();
        if (invalidScopes.Any())
            throw new DomainException("token", ErrorCodeType.SCOPE_NOT_ALLOWED, $"Scope(s) not allowed: {string.Join(", ", invalidScopes)}");

        var finalScopes = requestedScopes.Distinct(StringComparer.Ordinal).ToArray();
        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(client.AccessTokenLifetime);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, null, finalScopes, "client_credentials", ipAddress, userAgent, expiresIn);

        await _unitOfWork.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            Scope = string.Join(" ", finalScopes)
        };
    }

    public async Task<TokenResponse> RefreshTokenGrantAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            throw new DomainException("token", "refresh_token is required.");

        var client = await _clientAuth.AuthenticateAsync(clientId ?? request.ClientId, clientSecret);
        var rt = await _refreshTokenProcessor.ValidateActiveAsync(
            request.RefreshToken,
            "token",
            "The refresh token is invalid.",
            "The refresh token has expired.");

        if (rt.ClientId != client.Id)
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "Refresh token was not issued to this client.");

        // RFC 6749 §6: client MAY request a narrower scope on refresh; expansion is not permitted.
        string[]? narrowedScopes = null;
        if (!string.IsNullOrWhiteSpace(request.Scope))
        {
            var requestedScopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var exceeded = requestedScopes.Except(rt.Scopes, StringComparer.Ordinal).ToArray();
            if (exceeded.Any())
                throw new DomainException("token", ErrorCodeType.INVALID_SCOPE,
                    $"Requested scope exceeds the scope of the original grant: {string.Join(", ", exceeded)}");
            narrowedScopes = requestedScopes;
        }

        return await _refreshTokenProcessor.RotateAsync(
            rt,
            client,
            client.RefreshTokenLifetime,
            ipAddress,
            userAgent,
            narrowedScopes);
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
            throw new DomainException("userinfo", ErrorCodeType.INVALID_TOKEN, "Invalid or expired access token.");

        var grantedScopes = (introspect.Scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!grantedScopes.Contains("openid", StringComparer.Ordinal))
            throw new DomainException("userinfo", ErrorCodeType.SCOPE_NOT_ALLOWED, "openid scope is required for the UserInfo endpoint.");

        if (!Guid.TryParse(introspect.Sub, out var userId))
            throw new DomainException("userinfo", ErrorCodeType.INVALID_TOKEN, "Invalid subject claim.");

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new DomainException("userinfo", ErrorCodeType.INVALID_TOKEN, "User not found.");

        return new UserInfoResponse
        {
            Sub = user.Id.ToString(),
            Name = grantedScopes.Contains("profile", StringComparer.Ordinal) ? user.DisplayName : null,
            Email = grantedScopes.Contains("email", StringComparer.Ordinal) ? user.Email : null,
            EmailVerified = grantedScopes.Contains("email", StringComparer.Ordinal) ? user.EmailVerified : null,
            PhoneNumber = grantedScopes.Contains("phone", StringComparer.Ordinal) ? user.PhoneNumber : null,
            PhoneNumberVerified = grantedScopes.Contains("phone", StringComparer.Ordinal) ? user.PhoneVerified : null,
            Locale = grantedScopes.Contains("profile", StringComparer.Ordinal) ? user.LocaleCode : null,
            Zoneinfo = grantedScopes.Contains("profile", StringComparer.Ordinal) ? user.Zoneinfo : null,
            // OIDC Core §5.1: updated_at is a Unix timestamp
            UpdatedAt = grantedScopes.Contains("profile", StringComparer.Ordinal)
                ? new DateTimeOffset(user.UpdatedAt).ToUnixTimeSeconds()
                : null
        };
    }

    public OpenIdConfigurationResponse GetOpenIdConfiguration(string baseUrl)
    {
        return new OpenIdConfigurationResponse
        {
            Issuer = _tokenService.GetIssuer(),
            AuthorizationEndpoint = $"{baseUrl}/authorize",
            TokenEndpoint = $"{baseUrl}/token",
            UserInfoEndpoint = $"{baseUrl}/oauth2/userinfo",
            JwksUri = $"{baseUrl}/.well-known/jwks.json",
            RevocationEndpoint = $"{baseUrl}/revoke",
            IntrospectionEndpoint = $"{baseUrl}/introspect",
            EndSessionEndpoint = $"{baseUrl}/oauth2/end-session",
            ResponseTypesSupported = new[] { "code" },
            // OIDC Core §3.1.2.1: only 'query' is supported for code flow
            ResponseModesSupported = new[] { "query" },
            GrantTypesSupported = new[] { "authorization_code", "client_credentials", "refresh_token" },
            SubjectTypesSupported = new[] { "public" },
            IdTokenSigningAlgValuesSupported = new[] { "RS256" },
            // 'none' is required for PUBLIC clients (OAuth2.1 §2.1)
            TokenEndpointAuthMethodsSupported = new[] { "none", "client_secret_basic", "client_secret_post" },
            ScopesSupported = new[] { "openid", "profile", "email", "phone", "offline_access" },
            ClaimsSupported = new[]
            {
                // JWT registered claims
                "sub", "iss", "aud", "iat", "exp", "jti",
                // OIDC protocol claims
                "auth_time", "nonce", "at_hash",
                // profile scope (OIDC Core §5.1)
                "name", "locale", "zoneinfo", "updated_at",
                // email scope
                "email", "email_verified",
                // phone scope
                "phone_number", "phone_number_verified"
            },
            CodeChallengeMethodsSupported = new[] { "S256" },
            // PKCE is mandatory on this server for all public clients
            RequirePkce = true,
            RequestParameterSupported = false
        };
    }

    public AuthorizationServerMetadataResponse GetAuthorizationServerMetadata(string baseUrl)
    {
        return new AuthorizationServerMetadataResponse
        {
            Issuer = _tokenService.GetIssuer(),
            AuthorizationEndpoint = $"{baseUrl}/authorize",
            TokenEndpoint = $"{baseUrl}/token",
            JwksUri = $"{baseUrl}/.well-known/jwks.json",
            RevocationEndpoint = $"{baseUrl}/revoke",
            IntrospectionEndpoint = $"{baseUrl}/introspect",
            ResponseTypesSupported = new[] { "code" },
            GrantTypesSupported = new[] { "authorization_code", "client_credentials", "refresh_token" },
            // 'none' is required for PUBLIC clients (OAuth2.1 §2.1)
            TokenEndpointAuthMethodsSupported = new[] { "none", "client_secret_basic", "client_secret_post" },
            CodeChallengeMethodsSupported = new[] { "S256" }
        };
    }

    public async Task<string?> EndSessionAsync(Guid? userId, string? idTokenHint, string? postLogoutRedirectUri, string? clientId = null)
    {
        if (userId.HasValue)
            await _tokenService.RevokeAllUserTokensAsync(userId.Value, sessionId: null, reason: "end_session");

        if (string.IsNullOrEmpty(postLogoutRedirectUri))
            return null;

        // Resolve client_id: prefer explicit parameter, then parse the id_token_hint JWT as an
        // unauthenticated hint (OIDC Session Management §2.1 — no signature verification required
        // because the hint is advisory only; we just need the "aud" claim to look up the client).
        var resolvedClientId = clientId;
        if (string.IsNullOrEmpty(resolvedClientId) && !string.IsNullOrEmpty(idTokenHint))
        {
            try
            {
                var parts = idTokenHint.Split('.');
                if (parts.Length == 3)
                {
                    var padded = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
                    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("aud", out var aud))
                        resolvedClientId = aud.ValueKind == System.Text.Json.JsonValueKind.Array
                            ? aud.EnumerateArray().FirstOrDefault().GetString()
                            : aud.GetString();
                }
            }
            catch { /* malformed token hint — ignore */ }
        }

        if (string.IsNullOrEmpty(resolvedClientId))
            return null; // No client context — cannot safely validate the redirect URI.

        AuthorizationClient? client;
        try { client = await _clientAuth.LoadAsync(resolvedClientId); }
        catch { return null; }

        var allowed = client.PostLogoutRedirectUris ?? Array.Empty<string>();
        return allowed.Any(u => string.Equals(u, postLogoutRedirectUri, StringComparison.Ordinal))
            ? postLogoutRedirectUri
            : null;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────────
    // (All client auth and scope logic has moved to ClientAuthenticator)

    private static string BuildCodeRedirectUrl(string redirectUri, string code, string? state)
    {
        var url = $"{redirectUri}?code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state))
            url += $"&state={Uri.EscapeDataString(state)}";
        return url;
    }

    private static string BuildErrorRedirectUrl(string redirectUri, string error, string? errorDescription, string? state)
    {
        var url = $"{redirectUri}?error={Uri.EscapeDataString(error)}";
        if (!string.IsNullOrEmpty(errorDescription))
            url += $"&error_description={Uri.EscapeDataString(errorDescription)}";
        if (!string.IsNullOrEmpty(state))
            url += $"&state={Uri.EscapeDataString(state)}";
        return url;
    }
}
