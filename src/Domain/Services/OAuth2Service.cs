using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ClientAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using Microsoft.Extensions.Caching.Distributed;

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
    private readonly IAuditLogRepository _auditLogRepo;
    private readonly IDistributedCache _cache;
    private readonly IClientService _clientService;
    private readonly string _authIssuer;

    private const int DeviceCodeExpirySeconds = 1800; // 30 min
    private const int DeviceCodePollingInterval = 5;
    private const int ParExpirySeconds = 90; // RFC 9126 §2.1

    public OAuth2Service(
        IUnitOfWork unitOfWork,
        IAuthorizationCodeRepository authCodeRepo,
        IAuthorizationConsentRepository consentRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUserRepository userRepo,
        ITokenService tokenService,
        IClientAuthenticator clientAuth,
        IPasswordService passwordService,
        IRefreshTokenProcessor refreshTokenProcessor,
        IAuditLogRepository auditLogRepo,
        IClientService clientService,
        IDistributedCache? cache = null,
        string authIssuer = "")
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
        _auditLogRepo = auditLogRepo;
        _clientService = clientService;
        _cache = cache!;
        _authIssuer = authIssuer;
    }

    public async Task<AuthorizeEndpointResponse> ProcessAuthorizeRequestAsync(AuthorizeRequest request, Guid? authenticatedUserId)
    {
        // RFC 9126 §4: if request_uri is present, fetch the cached PAR parameters and replace the
        // inline request fields. The request_uri is single-use — remove it from the cache on access.
        if (!string.IsNullOrEmpty(request.RequestUri))
        {
            if (_cache == null)
                return new AuthorizeEndpointResponse { Kind = AuthorizeResponseKind.Error, Error = "server_error", ErrorDescription = "Cache not available." };

            var parJson = await _cache.GetStringAsync(ParKey(request.RequestUri));
            if (string.IsNullOrEmpty(parJson))
                return new AuthorizeEndpointResponse { Kind = AuthorizeResponseKind.Error, Error = "invalid_request_uri", ErrorDescription = "request_uri is expired or invalid." };

            await _cache.RemoveAsync(ParKey(request.RequestUri));
            var stored = System.Text.Json.JsonSerializer.Deserialize<AuthorizeRequest>(parJson)!;
            // Overlay only the fields from the cached object; allow the client_id from the URL
            // to be overridden by the authenticated one stored in the PAR object.
            request = stored;
        }

        var validation = await ValidateAuthorizeRequestAsync(request, authenticatedUserId ?? Guid.Empty);
        if (!validation.IsValid)
        {
            // RFC 6749 §4.1.2.1: only redirect the error back when redirect_uri was already
            // validated against the client's registered list. For errors that occur before that
            // check (missing/bad client_id, bad response_type) the redirect_uri is still
            // attacker-controlled — redirecting would be an open redirect vulnerability.
            if (validation.RedirectUriValidated && !string.IsNullOrEmpty(request.RedirectUri))
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

        if (!client.IsRedirectUriRegistered(request.RedirectUri))
            return AuthorizeValidationResult.Fail("invalid_request", "redirect_uri does not exactly match a registered URI.");

        if (string.IsNullOrEmpty(request.CodeChallenge))
            return AuthorizeValidationResult.FailSafeRedirect("invalid_request", "code_challenge is required.");

        if (request.CodeChallengeMethod != "S256")
            return AuthorizeValidationResult.FailSafeRedirect("invalid_request", "code_challenge_method must be S256.");

        var requestedScopes = (request.Scope ?? "openid")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var allowedScopes = await _clientAuth.GetAllowedScopeNamesAsync(client.Id);

        var invalidScopes = requestedScopes.Except(allowedScopes).ToArray();
        if (invalidScopes.Any())
            return AuthorizeValidationResult.FailSafeRedirect("invalid_scope", $"Scope(s) not allowed: {string.Join(", ", invalidScopes)}");

        if (!client.IsGrantTypeAllowed("AUTHORIZATION_CODE"))
            return AuthorizeValidationResult.FailSafeRedirect("unauthorized_client", "Client is not authorized for authorization_code grant.");

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
                        return AuthorizeValidationResult.FailSafeRedirect("login_required",
                            "Authentication is required. Re-authenticate and retry.");
                    if (requiresConsent)
                        return AuthorizeValidationResult.FailSafeRedirect("consent_required",
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

        _auditLogRepo.Insert(new AuditLog
        {
            ClientId = client.Id,
            UserId = userId,
            ActorType = "user",
            Action = "auth_code_issued",
            EntityName = "AuthorizationCode",
            Result = "success"
        });

        await _unitOfWork.SaveChangesAsync();
        return code;
    }

    public async Task<TokenResponse> ProcessTokenRequestAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null)
    {
        var normalizedGrantType = request.GrantType?.Trim().ToUpperInvariant();
        return normalizedGrantType switch
        {
            "AUTHORIZATION_CODE" => await ExchangeAuthorizationCodeAsync(request, clientId, clientSecret, ipAddress, userAgent, dpopJkt),
            "CLIENT_CREDENTIALS" => await ClientCredentialsGrantAsync(request, clientId!, clientSecret, ipAddress, userAgent, dpopJkt),
            "REFRESH_TOKEN"      => await RefreshTokenGrantAsync(request, clientId, clientSecret, ipAddress, userAgent, dpopJkt),
            "URN:IETF:PARAMS:OAUTH:GRANT-TYPE:DEVICE_CODE" => await DeviceCodeGrantAsync(request, clientId, clientSecret, ipAddress, userAgent),
            "URN:IETF:PARAMS:OAUTH:GRANT-TYPE:TOKEN-EXCHANGE" => await TokenExchangeGrantAsync(request, clientId, clientSecret, ipAddress, userAgent, dpopJkt),
            // OAuth 2.1 explicitly removes password and implicit grants.
            "PASSWORD" => throw new DomainException("token", ErrorCodeType.UNSUPPORTED_GRANT_TYPE,
                "The 'password' grant has been removed in OAuth 2.1. Use 'authorization_code' with PKCE."),
            _ => throw new DomainException("token", ErrorCodeType.UNSUPPORTED_GRANT_TYPE)
        };
    }

    public async Task<TokenResponse> ExchangeAuthorizationCodeAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null)
    {
        var client = await _clientAuth.AuthenticateAsync(clientId ?? request.ClientId, clientSecret);

        if (string.IsNullOrEmpty(request.Code))
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "code is required.");

        if (string.IsNullOrEmpty(request.RedirectUri))
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "redirect_uri is required.");

        var codeHash = _tokenService.HashToken(request.Code);
        var authCode = await _authCodeRepo.GetByCodeHashAsync(codeHash);

        if (authCode == null || !authCode.IsValid)
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
            client.Id, authCode.UserId, scopes, "AUTHORIZATION_CODE", ipAddress, userAgent,
            expiresIn, dpopJkt);
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

        _auditLogRepo.Insert(new AuditLog
        {
            ClientId = client.Id,
            UserId = authCode.UserId,
            SessionId = authCode.SessionId,
            ActorType = "user",
            Action = "token_issued",
            EntityName = "AccessToken",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Result = "success",
            Metadata = $"{{\"grant\":\"authorization_code\",\"scopes\":\"{string.Join(" ", scopes)}\"}}"
        });

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
        TokenRequest request, string clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null)
    {
        var client = await _clientAuth.AuthenticateAsync(clientId, clientSecret);

        var allowedGrants = client.AllowedGrantTypes ?? Array.Empty<string>();
        if (!allowedGrants.Contains("CLIENT_CREDENTIALS", StringComparer.OrdinalIgnoreCase))
            throw new DomainException("token", ErrorCodeType.UNAUTHORIZED_CLIENT, "Client is not authorized for client_credentials grant.");

        var requestedScopes = (request.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!requestedScopes.Any())
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "scope is required for client_credentials grant.");

        // RFC 6749 §4.4 / OAuth 2.1 §4.4.3: MUST NOT issue refresh tokens for client_credentials.
        // Reject offline_access explicitly so clients are not misled by a scope that silently does nothing.
        if (requestedScopes.Contains("offline_access", StringComparer.Ordinal))
            throw new DomainException("token", ErrorCodeType.INVALID_SCOPE,
                "offline_access scope is not permitted for the client_credentials grant.");

        // OIDC scopes (openid, profile, email, phone) require a human subject (sub = user).
        // client_credentials tokens have sub = client_id — there is no user to authenticate or describe.
        // Allowing these scopes would produce a token indistinguishable from a user-delegated token
        // on resource servers that inspect scope names alone.
        var oidcOnlyScopes = new[] { "openid", "profile", "email", "phone" };
        var oidcRequested = requestedScopes.Intersect(oidcOnlyScopes, StringComparer.Ordinal).ToArray();
        if (oidcRequested.Any())
            throw new DomainException("token", ErrorCodeType.INVALID_SCOPE,
                $"OIDC scopes ({string.Join(", ", oidcRequested)}) are not permitted for the client_credentials grant. They require a human subject.");

        var allowedScopes = await _clientAuth.GetAllowedScopeNamesAsync(client.Id);

        var invalidScopes = requestedScopes.Except(allowedScopes).ToArray();
        if (invalidScopes.Any())
            throw new DomainException("token", ErrorCodeType.SCOPE_NOT_ALLOWED, $"Scope(s) not allowed: {string.Join(", ", invalidScopes)}");

        var finalScopes = requestedScopes.Distinct(StringComparer.Ordinal).ToArray();
        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(client.AccessTokenLifetime);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, null, finalScopes, "CLIENT_CREDENTIALS", ipAddress, userAgent, expiresIn, dpopJkt);

        _auditLogRepo.Insert(new AuditLog
        {
            ClientId = client.Id,
            ActorType = "client",
            Action = "token_issued",
            EntityName = "AccessToken",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Result = "success",
            Metadata = $"{{\"grant\":\"client_credentials\",\"scopes\":\"{string.Join(" ", finalScopes)}\"}}"
        });

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
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "refresh_token is required.");

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

        var tokenResponse = await _refreshTokenProcessor.RotateAsync(
            rt,
            client,
            client.RefreshTokenLifetime,
            ipAddress,
            userAgent,
            narrowedScopes,
            dpopJkt);

        _auditLogRepo.Insert(new AuditLog
        {
            ClientId = client.Id,
            UserId = rt.UserId != Guid.Empty ? rt.UserId : null,
            SessionId = rt.SessionId,
            ActorType = rt.UserId != Guid.Empty ? "user" : "client",
            Action = "token_refreshed",
            EntityName = "AccessToken",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Result = "success"
        });

        await _unitOfWork.SaveChangesAsync();
        return tokenResponse;
    }

    public async Task RevokeAsync(RevocationRequest request, string clientId, string? clientSecret)
    {
        var client = await _clientAuth.AuthenticateAsync(clientId, clientSecret);

        if (string.IsNullOrEmpty(request.Token))
            return; // RFC 7009: servers should not return an error for missing token

        await _tokenService.RevokeTokenAsync(request.Token, request.TokenTypeHint, client.Id, "revoked_by_client");

        _auditLogRepo.Insert(new AuditLog
        {
            ClientId = client.Id,
            ActorType = "client",
            Action = "token_revoked",
            EntityName = "Token",
            Result = "success",
            Metadata = request.TokenTypeHint != null ? $"{{\"token_type_hint\":\"{request.TokenTypeHint}\"}}" : null
        });

        await _unitOfWork.SaveChangesAsync();
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
            GrantTypesSupported = new[] { "authorization_code", "client_credentials", "refresh_token", "urn:ietf:params:oauth:grant-type:device_code", "urn:ietf:params:oauth:grant-type:token-exchange" },
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
            RequestParameterSupported = false,
            DeviceAuthorizationEndpoint = $"{baseUrl}/oauth2/device_authorization",
            PushedAuthorizationRequestEndpoint = $"{baseUrl}/oauth2/par",
            RegistrationEndpoint = $"{baseUrl}/oauth2/register",
            // RFC 9449: advertise DPoP as optional — clients that attach DPoP proofs get bound tokens.
            DPoPSigningAlgValuesSupported = new[] { "RS256", "RS384", "RS512", "ES256", "ES384", "ES512", "PS256", "PS384", "PS512" }
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
            GrantTypesSupported = new[] { "authorization_code", "client_credentials", "refresh_token", "urn:ietf:params:oauth:grant-type:device_code", "urn:ietf:params:oauth:grant-type:token-exchange" },
            // 'none' is required for PUBLIC clients (OAuth2.1 §2.1)
            TokenEndpointAuthMethodsSupported = new[] { "none", "client_secret_basic", "client_secret_post" },
            CodeChallengeMethodsSupported = new[] { "S256" },
            DeviceAuthorizationEndpoint = $"{baseUrl}/oauth2/device_authorization",
            PushedAuthorizationRequestEndpoint = $"{baseUrl}/oauth2/par",
            RegistrationEndpoint = $"{baseUrl}/oauth2/register",
            DPoPSigningAlgValuesSupported = new[] { "RS256", "RS384", "RS512", "ES256", "ES384", "ES512", "PS256", "PS384", "PS512" }
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

    // ─── RFC 8628 — Device Authorization Grant ────────────────────────────────

    private async Task<TokenResponse> DeviceCodeGrantAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent)
    {
        if (_cache == null)
            throw new DomainException("token", "Device code grant requires a distributed cache.");

        var deviceCode = request.Code
            ?? throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "device_code is required.");

        var entryJson = await _cache.GetStringAsync(DeviceCodeKey(deviceCode));

        if (string.IsNullOrEmpty(entryJson))
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "expired_token");

        var entry = System.Text.Json.JsonSerializer.Deserialize<DeviceCodeEntry>(entryJson)
            ?? throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "expired_token");

        if (entry.Status == "pending")
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "authorization_pending");

        if (entry.Status == "denied")
        {
            await _cache.RemoveAsync(DeviceCodeKey(deviceCode));
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "access_denied");
        }

        if (entry.Status != "approved" || !entry.UserId.HasValue)
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "authorization_pending");

        // Code is approved — exchange for tokens and remove from cache.
        await _cache.RemoveAsync(DeviceCodeKey(deviceCode));
        await _cache.RemoveAsync(UserCodeKey(entry.UserCode));

        var client = await _clientAuth.AuthenticateAsync(clientId ?? entry.ClientId, clientSecret);

        // C-4: Enforce AllowedGrantTypes for device code grant.
        if (!client.IsGrantTypeAllowed("DEVICE_CODE"))
            throw new DomainException("token", ErrorCodeType.UNAUTHORIZED_CLIENT, "Client is not authorized to use the device code grant.");

        var scopes = entry.Scopes;

        var atLifetime  = _tokenService.GetAccessTokenLifetimeSeconds();
        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, entry.UserId, scopes, "urn:ietf:params:oauth:grant-type:device_code", ipAddress, userAgent, atLifetime);
        (string refreshRaw, Guid _rtId) = await _tokenService.GenerateRefreshTokenAsync(
            client.Id, entry.UserId, null, scopes,
            lifetimeSeconds: 30 * 24 * 3600, ipAddress: ipAddress, userAgent: userAgent);

        // M-10: Audit log for device code token issuance.
        _auditLogRepo.Insert(new AuditLog
        {
            ClientId = client.Id,
            UserId = entry.UserId,
            ActorType = "device",
            Action = "token_issued",
            EntityName = "AccessToken",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Result = "success",
            Metadata = $"{{\"grant\":\"urn:ietf:params:oauth:grant-type:device_code\",\"scopes\":\"{string.Join(" ", scopes)}\"}}"
        });
        await _unitOfWork.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken  = accessToken,
            TokenType    = "Bearer",
            ExpiresIn    = atLifetime,
            RefreshToken = refreshRaw,
            Scope        = string.Join(" ", scopes),
        };
    }

    public async Task<DeviceAuthorizationResponse> DeviceAuthorizationAsync(
        DeviceAuthorizationRequest request, string? clientId, string? clientSecret)
    {
        if (_cache == null)
            throw new DomainException("device", "Device authorization requires a distributed cache.");

        var resolvedClientId = clientId ?? request.ClientId
            ?? throw new DomainException("device", "client_id is required.");

        // C-5: Authenticate client (confidential clients must supply their secret per RFC 8628 §3.1).
        var client = await _clientAuth.AuthenticateAsync(resolvedClientId, clientSecret);

        // C-6: Validate requested scopes against client's registered scopes.
        var allowedScopes = await _clientAuth.GetAllowedScopeNamesAsync(client.Id);
        var requestedScopes = request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            ?? Array.Empty<string>();
        var invalidScopes = requestedScopes.Where(s => !allowedScopes.Contains(s)).ToArray();
        if (invalidScopes.Length > 0)
            throw new DomainException("device", ErrorCodeType.INVALID_SCOPE,
                $"Requested scopes not allowed for this client: {string.Join(" ", invalidScopes)}");
        var grantedScopes = requestedScopes.Length > 0
            ? requestedScopes
            : allowedScopes.Where(s => s != "offline_access").ToArray();

        // Generate cryptographically random codes.
        var deviceCode = GenerateDeviceCode();
        var userCode   = GenerateUserCode();

        var entry = System.Text.Json.JsonSerializer.Serialize(new DeviceCodeEntry
        {
            ClientId  = resolvedClientId,
            UserCode  = userCode,
            Scopes    = grantedScopes,
            Status    = "pending",
            UserId    = null,
        });

        var opts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(DeviceCodeExpirySeconds)
        };

        // Store under both device_code key and user_code key so that both lookup directions work.
        await _cache.SetStringAsync(DeviceCodeKey(deviceCode), entry, opts);
        await _cache.SetStringAsync(UserCodeKey(userCode), deviceCode, opts);

        var verificationUri = $"{_authIssuer.TrimEnd('/')}/device";

        return new DeviceAuthorizationResponse
        {
            DeviceCode            = deviceCode,
            UserCode              = userCode,
            VerificationUri       = verificationUri,
            VerificationUriComplete = $"{verificationUri}?user_code={Uri.EscapeDataString(userCode)}",
            ExpiresIn             = DeviceCodeExpirySeconds,
            Interval              = DeviceCodePollingInterval,
        };
    }

    public async Task ApproveDeviceCodeAsync(string userCode, Guid userId, bool approved)
    {
        if (_cache == null)
            throw new DomainException("device", "Device authorization requires a distributed cache.");

        var deviceCode = await _cache.GetStringAsync(UserCodeKey(userCode))
            ?? throw new DomainException("device", "Invalid or expired user_code.");

        var entryJson = await _cache.GetStringAsync(DeviceCodeKey(deviceCode))
            ?? throw new DomainException("device", "Device code not found.");

        var entry = System.Text.Json.JsonSerializer.Deserialize<DeviceCodeEntry>(entryJson)
            ?? throw new DomainException("device", "Corrupt device code entry.");

        entry.Status = approved ? "approved" : "denied";
        entry.UserId = approved ? userId : null;

        // Keep same TTL semantics — refresh with a fixed 5-minute window for the device to pick up.
        var opts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(DeviceCodeKey(deviceCode),
            System.Text.Json.JsonSerializer.Serialize(entry), opts);
    }

    private static string GenerateDeviceCode()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GenerateUserCode()
    {
        // RFC 8628 §6.1: user codes should be short and easy to type.
        // Format: XXXX-XXXX (uppercase alphanum, excluding confusable chars 0/O/I/1)
        const string charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
        var chars = new char[9]; // 4 + dash + 4
        for (int i = 0; i < 4; i++) chars[i]   = charset[bytes[i]   % charset.Length];
        chars[4] = '-';
        for (int i = 0; i < 4; i++) chars[5+i] = charset[bytes[4+i] % charset.Length];
        return new string(chars);
    }

    private static string DeviceCodeKey(string deviceCode) => $"oauth2:device:{deviceCode}";
    private static string UserCodeKey(string userCode)     => $"oauth2:usercode:{userCode.Replace("-", "").ToUpperInvariant()}";
    private static string ParKey(string requestUri)        => $"oauth2:par:{requestUri}";

    // ─── RFC 9126 — Pushed Authorization Requests ────────────────────────────────────

    public async Task<PushedAuthorizationResponse> PushAuthorizationRequestAsync(
        PushedAuthorizationFormRequest form, string? clientId, string? clientSecret)
    {
        if (_cache == null)
            throw new DomainException("par", "PAR requires a distributed cache.");

        // Client MUST authenticate for PAR (RFC 9126 §2.1).
        var resolvedClientId = clientId ?? form.ClientId
            ?? throw new DomainException("par", "client_id is required.");
        await _clientAuth.AuthenticateAsync(resolvedClientId, clientSecret);

        // Basic validate: require response_type and redirect_uri to prevent storing garbage.
        if (string.IsNullOrEmpty(form.ResponseType))
            throw new DomainException("par", "response_type is required.");
        if (string.IsNullOrEmpty(form.RedirectUri))
            throw new DomainException("par", "redirect_uri is required.");

        var authorizeRequest = new AuthorizeRequest
        {
            ResponseType = form.ResponseType,
            ClientId = resolvedClientId,
            RedirectUri = form.RedirectUri,
            Scope = form.Scope,
            State = form.State,
            CodeChallenge = form.CodeChallenge,
            CodeChallengeMethod = form.CodeChallengeMethod,
            Nonce = form.Nonce,
            Prompt = form.Prompt,
            MaxAge = form.MaxAge,
            LoginHint = form.LoginHint
        };

        var requestUriId = Guid.NewGuid().ToString("N");
        var requestUri = $"urn:ietf:params:oauth:request_uri:{requestUriId}";

        var json = System.Text.Json.JsonSerializer.Serialize(authorizeRequest);
        await _cache.SetStringAsync(ParKey(requestUri), json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ParExpirySeconds)
        });

        return new PushedAuthorizationResponse { RequestUri = requestUri, ExpiresIn = ParExpirySeconds };
    }

    // ─── RFC 7591 — Dynamic Client Registration ─────────────────────────────────────

    public async Task<DynamicClientRegistrationResponse> RegisterClientDynamicallyAsync(
        DynamicClientRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientName))
            throw new DomainException("registration", "client_name is required.");
        if (request.RedirectUris == null || request.RedirectUris.Length == 0)
            throw new DomainException("registration", "redirect_uris is required and must not be empty.");

        var authMethod = request.TokenEndpointAuthMethod ?? "client_secret_basic";
        var clientType = authMethod == "none" ? "PUBLIC" : "CONFIDENTIAL";
        var grantTypes = request.GrantTypes ?? new[] { "authorization_code" };
        var responseTypes = request.ResponseTypes ?? new[] { "code" };

        // Derive scope ids from free-form scope string: map known names to ids via discovery.
        // For dynamic registration we accept the scope string as-is and store it as scope names.
        var scopeNames = request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var createRequest = new ClientCreateRequest
        {
            ClientName = request.ClientName,
            ClientType = clientType,
            TokenEndpointAuthMethod = authMethod.ToUpperInvariant().Replace("-", "_"),
            RequirePkce = request.RequirePkce ?? true,
            RequireConsent = true,
            RedirectUris = request.RedirectUris,
            AllowedGrantTypes = grantTypes.Select(g => g.ToUpperInvariant().Replace("-", "_")).ToArray(),
            AllowedResponseTypes = responseTypes,
            LogoUri = request.LogoUri,
            ClientUri = request.ClientUri,
            JwksUri = request.JwksUri
        };

        var (clientResult, secretResult) = await _clientService.CreateAsync(createRequest, "dynamic_registration");

        return new DynamicClientRegistrationResponse
        {
            ClientId = clientResult.ClientId,
            ClientSecret = secretResult.ClientSecret,
            ClientName = clientResult.ClientName,
            RedirectUris = clientResult.RedirectUris,
            GrantTypes = clientResult.AllowedGrantTypes ?? Array.Empty<string>(),
            ResponseTypes = clientResult.AllowedResponseTypes ?? Array.Empty<string>(),
            TokenEndpointAuthMethod = clientResult.TokenEndpointAuthMethod,
            Scope = request.Scope,
            LogoUri = clientResult.LogoUri,
            ClientUri = clientResult.ClientUri,
            JwksUri = request.JwksUri
        };
    }

    // ─── RFC 8693 — Token Exchange ─────────────────────────────────────────────────────

    public async Task<TokenResponse> TokenExchangeGrantAsync(
        TokenRequest request, string? clientId, string? clientSecret, string? ipAddress, string? userAgent, string? dpopJkt = null)
    {
        var client = await _clientAuth.AuthenticateAsync(clientId ?? request.ClientId, clientSecret);

        if (string.IsNullOrEmpty(request.SubjectToken))
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST, "subject_token is required.");

        // Only access_token subject token type is supported in this implementation.
        var subjectTokenType = request.SubjectTokenType
            ?? "urn:ietf:params:oauth:token-type:access_token";
        if (!subjectTokenType.Equals("urn:ietf:params:oauth:token-type:access_token", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("token", ErrorCodeType.INVALID_REQUEST,
                "Only urn:ietf:params:oauth:token-type:access_token is supported as subject_token_type.");

        // Validate the subject token.
        var introspect = await _tokenService.IntrospectTokenAsync(request.SubjectToken, "access_token");
        if (!introspect.Active)
            throw new DomainException("token", ErrorCodeType.INVALID_GRANT, "subject_token is invalid or expired.");

        // Determine the subject (may be a user or service account).
        Guid? subjectUserId = Guid.TryParse(introspect.Sub, out var uid) ? uid : null;

        // Scope: use requested or inherit from subject token.
        var originalScopes = (introspect.Scope ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var requestedScopes = !string.IsNullOrWhiteSpace(request.Scope)
            ? request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            : originalScopes;

        // Requested scopes must not exceed the subject token's scopes.
        var exceeded = requestedScopes.Except(originalScopes, StringComparer.Ordinal).ToArray();
        if (exceeded.Any())
            throw new DomainException("token", ErrorCodeType.INVALID_SCOPE,
                $"Requested scope exceeds subject_token scope: {string.Join(" ", exceeded)}");

        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(client.AccessTokenLifetime);
        var newAccessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, subjectUserId, requestedScopes,
            "urn:ietf:params:oauth:grant-type:token-exchange", ipAddress, userAgent, expiresIn, dpopJkt);

        _auditLogRepo.Insert(new AuditLog
        {
            ClientId = client.Id,
            UserId = subjectUserId,
            ActorType = subjectUserId.HasValue ? "user" : "client",
            Action = "token_exchanged",
            EntityName = "AccessToken",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Result = "success",
            Metadata = $"{{\"grant\":\"token_exchange\",\"scopes\":\"{string.Join(" ", requestedScopes)}\"}}"
        });
        await _unitOfWork.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            Scope = string.Join(" ", requestedScopes),
            IssuedTokenType = "urn:ietf:params:oauth:token-type:access_token"
        };
    }

    private sealed class DeviceCodeEntry
    {
        public string ClientId { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string[] Scopes { get; set; } = Array.Empty<string>();
        public string Status { get; set; } = "pending"; // pending | approved | denied
        public Guid? UserId { get; set; }
    }
}
