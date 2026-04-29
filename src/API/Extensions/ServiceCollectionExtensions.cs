using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.PasskeyAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.SamlAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.WebhookAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Services;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.Auth.Infrastructure.ExternalApis;
using MonkoraEdge.Core.Auth.Infrastructure.Configurations;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.Auth.Infrastructure.Extensions;
using MonkoraEdge.Core.Auth.Infrastructure.Services;
using MonkoraEdge.Core.Auth.Infrastructure.Services.Security;
using MonkoraEdge.Core.Auth.Infrastructure.Repositories;
using Fido2NetLib;
using MonkoraEdge.Core.DotNet.Infrastructure;
using Microsoft.Extensions.Configuration;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Cryptography;
using MonkoraEdge.Core.Auth.API.Middleware;
using DomainIUnitOfWork = MonkoraEdge.Core.Auth.Domain.Services.Interface.IUnitOfWork;
using DotNetIUnitOfWork = MonkoraEdge.Core.DotNet.Infrastructure.Interfaces.IUnitOfWork;


namespace MonkoraEdge.Core.Auth.API.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomConfigurations(this IServiceCollection services, EnvironmentOptions options)
    {   
        services.AddCustomDbContext(options.POSTGRES_CONNECTIONSTRING);
        services.AddScoped<DbContext>(m => m.GetService<AuthenticationDbContext>());
        services.AddScoped<UnitOfWork>();
        services.AddScoped<DotNetIUnitOfWork>(m => m.GetRequiredService<UnitOfWork>());
        services.AddScoped<DomainIUnitOfWork, DomainUnitOfWorkAdapter>();
        services.AddCustomHttpClients(options);

        #region Repositories
        
        services.AddScoped<ITenantRepository, TenantRepository>();

        // User aggregate
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserFileRepository, UserFileRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
        services.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();
        services.AddScoped<IUserSessionDeviceRepository, UserSessionDeviceRepository>();
        services.AddScoped<IUserTwoFactorSettingRepository, UserTwoFactorSettingRepository>();
        services.AddScoped<IUserTwoFactorRecoveryCodeRepository, UserTwoFactorRecoveryCodeRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();
        services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();

        // Provider aggregate
        services.AddScoped<IProviderRepository, ProviderRepository>();

        // Authorization aggregate
        services.AddScoped<IAuthorizationClientRepository, AuthorizationClientRepository>();
        services.AddScoped<IScopeRepository, ScopeRepository>();
        services.AddScoped<IAuthorizationClientScopeRepository, AuthorizationClientScopeRepository>();
        services.AddScoped<IAuthorizationCodeRepository, AuthorizationCodeRepository>();
        services.AddScoped<IAuthorizationCodeScopeRepository, AuthorizationCodeScopeRepository>();
        services.AddScoped<IAccessTokenRepository, AccessTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthorizationConsentRepository, AuthorizationConsentRepository>();
        services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();

        // Role aggregate
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        // Audit aggregate
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();
        services.AddScoped<IRateLimitRepository, RateLimitRepository>();

        // Tenant context — one instance per request, populated by TenantContextMiddleware.
        services.AddScoped<ITenantContext, RequestTenantContext>();

        // ApiKey aggregate
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        // Agreement aggregate
        services.AddScoped<IAgreementRepository, AgreementRepository>();
        services.AddScoped<IAgreementAcceptRepository, AgreementAcceptRepository>();

        // Webhook aggregate
        services.AddScoped<IWebhookEndpointRepository, WebhookEndpointRepository>();
        services.AddScoped<IWebhookDeliveryLogRepository, WebhookDeliveryLogRepository>();

        // Passkey aggregate
        services.AddScoped<IPasskeyCredentialRepository, PasskeyCredentialRepository>();

        // SAML 2.0 aggregate
        services.AddScoped<ISamlProviderRepository, SamlProviderRepository>();

        #endregion


        #region Services     

        services.AddTransient<ITenantService>(m => new TenantService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<ITenantRepository>()));

        // Stateless utility services
        services.AddSingleton<IPasswordService, PasswordService>();
        // Redis-backed challenge store — survives node restarts and is visible across all
        // instances. Requires IDistributedCache (Redis) to be registered, which is done via
        // AddStackExchangeRedisCache in Program.cs.
        services.AddSingleton<ITwoFactorChallengeStore, RedisTwoFactorChallengeStore>();

        services.AddScoped<IRefreshTokenProcessor>(m => new RefreshTokenProcessor(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IRefreshTokenRepository>(),
            m.GetRequiredService<ITokenService>()));

        // ── JWT signing key ──────────────────────────────────────────────────────
        // Registered as singleton: RSA key material is loaded from PEM once at startup,
        // validated for minimum key size, and shared across all requests.
        services.AddSingleton(m =>
        {
            var opts = m.GetRequiredService<EnvironmentOptions>();
            var rsa = RSA.Create();
            rsa.ImportFromPem(opts.OAUTH2_SIGNED_PRIVATE_KEY);
            if (rsa.KeySize < 2048)
                throw new InvalidOperationException(
                    $"OAUTH2_SIGNED_PRIVATE_KEY is {rsa.KeySize} bits. RS256 requires at least 2048 bits.");

            // Compute key identifier from public key material (first 16 bytes of SHA-256(N || e)).
            var p = rsa.ExportParameters(false);
            var keyMaterial = (p.Modulus ?? []).Concat(p.Exponent ?? []).ToArray();
            var kid = Convert.ToBase64String(SHA256.HashData(keyMaterial), 0, 16)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            return new RsaSecurityKey(rsa) { KeyId = kid };
        });

        // DPoP service (RFC 9449) — validates DPoP proof JWTs on the token endpoint
        services.AddScoped<IDPoPService>(m =>
            new MonkoraEdge.Core.Auth.Infrastructure.Services.Security.DPoPService(
                m.GetRequiredService<IDistributedCache>()));

        // FIDO2 / Passkey (WebAuthn Level 3)
        // ServerDomain = RP ID (effective domain of the relying party).
        // Origins = allowed origins — must match the browser's window.location.origin.
        services.AddSingleton<Fido2>(m =>
        {
            var config = m.GetRequiredService<IConfiguration>();
            return new Fido2(new Fido2Configuration
            {
                ServerDomain          = config["Fido2:ServerDomain"] ?? "localhost",
                ServerName            = config["Fido2:ServerName"]   ?? "MonkoraEdge Auth",
                Origins               = new HashSet<string>(config.GetSection("Fido2:Origins").Get<string[]>() ?? ["https://localhost:5001"]),
                TimestampDriftTolerance = 300_000   // 5 minutes in milliseconds
            });
        });

        // Passkey service (scoped — depends on scoped repositories)
        services.AddScoped<IPasskeyService>(m => new PasskeyService(
            m.GetRequiredService<Fido2>(),
            m.GetRequiredService<IDistributedCache>(),
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IPasskeyCredentialRepository>(),
            m.GetRequiredService<IUserRepository>(),
            m.GetRequiredService<IUserRoleRepository>(),
            m.GetRequiredService<IRoleRepository>(),
            m.GetRequiredService<ILoginAttemptRepository>(),
            m.GetRequiredService<ITokenService>()));

        // SAML 2.0 service (scoped — depends on scoped repositories)
        services.AddScoped<ISamlService>(m =>
        {
            var opts = m.GetRequiredService<EnvironmentOptions>();
            return new SamlService(
                m.GetRequiredService<IDistributedCache>(),
                m.GetRequiredService<DomainIUnitOfWork>(),
                m.GetRequiredService<ISamlProviderRepository>(),
                m.GetRequiredService<IUserRepository>(),
                m.GetRequiredService<IUserRoleRepository>(),
                m.GetRequiredService<IRoleRepository>(),
                m.GetRequiredService<ILoginAttemptRepository>(),
                m.GetRequiredService<ITokenService>(),
                opts.AES_ENCRYPTION_KEY);
        });

        // Token service (scoped — depends on scoped repositories)
        services.AddScoped<ITokenService>(m =>
        {
            var opts = m.GetRequiredService<EnvironmentOptions>();
            var audience = opts.OAUTH2_AUDIENCE;

            // Load optional previous signing key for JWKS publication during rotation window.
            // Old tokens stay verifiable until they expire (≤ 900 s), then remove the env var.
            RsaSecurityKey? previousKey = null;
            if (!string.IsNullOrEmpty(opts.OAUTH2_PREVIOUS_PRIVATE_KEY))
            {
                try
                {
                    var prevRsa = RSA.Create();
                    prevRsa.ImportFromPem(opts.OAUTH2_PREVIOUS_PRIVATE_KEY);
                    var p = prevRsa.ExportParameters(false);
                    var km = (p.Modulus ?? []).Concat(p.Exponent ?? []).ToArray();
                    var prevKid = Convert.ToBase64String(SHA256.HashData(km), 0, 16)
                        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
                    previousKey = new RsaSecurityKey(prevRsa) { KeyId = prevKid };
                }
                catch { /* ignore malformed previous key — log in production via ILogger */ }
            }

            return new TokenService(
                m.GetRequiredService<DomainIUnitOfWork>(),
                m.GetRequiredService<IAccessTokenRepository>(),
                m.GetRequiredService<IRefreshTokenRepository>(),
                m.GetRequiredService<IAuthorizationCodeRepository>(),
                m.GetRequiredService<IRevokedTokenRepository>(),
                m.GetRequiredService<IUserRepository>(),
                m.GetRequiredService<RsaSecurityKey>(),
                opts.AUTH_ISSUER,
                audience,
                opts.TOKEN_EXPIRES_IN_MINUTES * 60,
                opts.HASH_SECRET_KEY,
                previousKey);
        });

        // ── JWT Bearer authentication ──────────────────────────────────────────────────
        // Validates JWT access tokens on [Authorize] endpoints.
        // The RsaSecurityKey singleton is resolved via Configure<T> DI binding so the
        // validation key is always the same object that signs tokens — no double PEM load.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<RsaSecurityKey, EnvironmentOptions>((jwtOptions, rsaKey, opts) =>
            {
                var audience = opts.OAUTH2_AUDIENCE;

                // Build signing key list — current key + optional previous key for rotation window.
                var signingKeys = new List<SecurityKey> { rsaKey };
                if (!string.IsNullOrEmpty(opts.OAUTH2_PREVIOUS_PRIVATE_KEY))
                {
                    try
                    {
                        var prevRsa = RSA.Create();
                        prevRsa.ImportFromPem(opts.OAUTH2_PREVIOUS_PRIVATE_KEY);
                        var p = prevRsa.ExportParameters(false);
                        var km = (p.Modulus ?? []).Concat(p.Exponent ?? []).ToArray();
                        var prevKid = Convert.ToBase64String(SHA256.HashData(km), 0, 16)
                            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
                        signingKeys.Add(new RsaSecurityKey(prevRsa) { KeyId = prevKid });
                    }
                    catch { /* ignore malformed previous key */ }
                }

                // Capture HMAC key bytes for use in the OnTokenValidated closure below.
                // The same keyed hash function is used by TokenService.HashToken so DB lookups match.
                var hashKeyBytes = System.Text.Encoding.UTF8.GetBytes(opts.HASH_SECRET_KEY ?? string.Empty);

                jwtOptions.MapInboundClaims = false; // keep claims as-is; do not remap to WS-Security URIs

                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    // Use the plural form so the validator tries all keys — supports key rotation.
                    IssuerSigningKeys       = signingKeys,

                    // Restrict accepted algorithms — prevents alg=none and HS256 confusion attacks.
                    ValidAlgorithms         = new[] { SecurityAlgorithms.RsaSha256 },

                    // RFC 9068 §2.1: only accept tokens with typ=at+JWT; rejects ID tokens /
                    // generic JWTs from being used as access tokens on protected endpoints.
                    ValidTypes              = new[] { "at+JWT" },

                    ValidateIssuer          = true,
                    ValidIssuer             = opts.AUTH_ISSUER,

                    ValidateAudience        = true,
                    ValidAudience           = audience,

                    ValidateLifetime        = true,
                    // Zero clock skew: access tokens are capped at 900 s.
                    // A default 5-minute grace window would extend effective lifetime by 33%.
                    ClockSkew               = TimeSpan.Zero,

                    // Map JWT sub → User.Identity.Name and keep role claim readable.
                    NameClaimType           = "sub",
                    RoleClaimType           = "roles"
                };

                // ── Token revocation check ────────────────────────────────────────────
                // JWT signature + lifetime validation alone cannot detect a token that was
                // explicitly revoked via POST /revoke before its natural expiry (up to 900 s).
                // OnTokenValidated runs after cryptographic validation succeeds — we look up
                // the token hash in the DB revocation list to close the replay window.
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        // Resolve scoped repository from the per-request service container.
                        var revokedRepo = ctx.HttpContext.RequestServices
                            .GetRequiredService<IRevokedTokenRepository>();
                        var accessTokenRepo = ctx.HttpContext.RequestServices
                            .GetRequiredService<IAccessTokenRepository>();

                        var rawToken = ctx.SecurityToken.UnsafeToString();
                        // HMAC-SHA256 must match the algorithm used in TokenService.HashToken.
                        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
                        var tokenHash = hashKeyBytes.Length > 0
                            ? System.Convert.ToHexString(
                                System.Security.Cryptography.HMACSHA256.HashData(hashKeyBytes, tokenBytes))
                                .ToLowerInvariant()
                            : System.Convert.ToHexString(
                                System.Security.Cryptography.SHA256.HashData(tokenBytes))
                                .ToLowerInvariant();

                        // Check explicit revocation list first (covers RFC 7009 POST /revoke).
                        if (await revokedRepo.IsRevokedAsync(tokenHash))
                        {
                            ctx.Fail("Token has been revoked.");
                            return;
                        }

                        // Also check access token table for soft-delete via RevokedAt column —
                        // covers mass logout (RevokeAllUserTokensAsync) and session termination.
                        var accessToken = await accessTokenRepo.GetByTokenHashAsync(tokenHash);
                        if (accessToken?.RevokedAt.HasValue == true)
                        {
                            ctx.Fail("Token has been revoked.");
                        }
                    }
                };
            });

        services.AddAuthorization();

        // Auth service
        services.AddScoped<IAuthService>(m =>
        {
            var opts = m.GetRequiredService<EnvironmentOptions>();
            return new AuthService(
                m.GetRequiredService<DomainIUnitOfWork>(),
                m.GetRequiredService<IUserRepository>(),
                m.GetRequiredService<IUserIdentityRepository>(),
                m.GetRequiredService<IUserTwoFactorSettingRepository>(),
                m.GetRequiredService<IUserTwoFactorRecoveryCodeRepository>(),
                m.GetRequiredService<IEmailVerificationRepository>(),
                m.GetRequiredService<IPasswordResetRepository>(),
                m.GetRequiredService<IPasswordHistoryRepository>(),
                m.GetRequiredService<ILoginAttemptRepository>(),
                m.GetRequiredService<IAuditLogRepository>(),
                m.GetRequiredService<IUserRoleRepository>(),
                m.GetRequiredService<IRoleRepository>(),
                m.GetRequiredService<IRefreshTokenRepository>(),
                m.GetRequiredService<ITokenService>(),
                m.GetRequiredService<IPasswordService>(),
                m.GetRequiredService<IAuthorizationClientRepository>(),
                m.GetRequiredService<ITwoFactorChallengeStore>(),
                m.GetRequiredService<INotificationApi>(),
                opts.SIGNIN_FAILED_IN_MINUTES);
        });

        // Client authenticator — handles client verification and scope resolution
        services.AddScoped<IClientAuthenticator>(m => new ClientAuthenticator(
            m.GetRequiredService<IAuthorizationClientRepository>(),
            m.GetRequiredService<IAuthorizationClientScopeRepository>(),
            m.GetRequiredService<IScopeRepository>(),
            m.GetRequiredService<IPasswordService>()));

        // OAuth2 service
        services.AddScoped<IOAuth2Service>(m =>
        {
            var opts = m.GetRequiredService<EnvironmentOptions>();
            return new OAuth2Service(
                m.GetRequiredService<DomainIUnitOfWork>(),
                m.GetRequiredService<IAuthorizationCodeRepository>(),
                m.GetRequiredService<IAuthorizationConsentRepository>(),
                m.GetRequiredService<IRefreshTokenRepository>(),
                m.GetRequiredService<IUserRepository>(),
                m.GetRequiredService<ITokenService>(),
                m.GetRequiredService<IClientAuthenticator>(),
                m.GetRequiredService<IPasswordService>(),
                m.GetRequiredService<IRefreshTokenProcessor>(),
                m.GetRequiredService<IAuditLogRepository>(),
                m.GetRequiredService<IClientService>(),
                m.GetRequiredService<IDistributedCache>(),
                opts.AUTH_ISSUER);
        });

        // Social login service (OAuth2/OIDC external provider flow)
        services.AddScoped<ISocialLoginService>(m =>
        {
            var opts = m.GetRequiredService<EnvironmentOptions>();
            return new SocialLoginService(
                m.GetRequiredService<DomainIUnitOfWork>(),
                m.GetRequiredService<IAuthorizationClientRepository>(),
                m.GetRequiredService<IProviderRepository>(),
                m.GetRequiredService<IUserRepository>(),
                m.GetRequiredService<IUserExternalLoginRepository>(),
                m.GetRequiredService<ITokenService>(),
                m.GetRequiredService<IDistributedCache>(),
                m.GetRequiredService<IHttpClientFactory>(),
                opts.AES_ENCRYPTION_KEY,
                m.GetRequiredService<ILogger<SocialLoginService>>());
        });

        // User service
        services.AddScoped<IUserService>(m => new UserService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IUserRepository>(),
            m.GetRequiredService<IUserIdentityRepository>(),
            m.GetRequiredService<IUserRoleRepository>(),
            m.GetRequiredService<IRoleRepository>(),
            m.GetRequiredService<IPasswordService>(),
            m.GetRequiredService<IUserSessionRepository>(),
            m.GetRequiredService<IUserSessionDeviceRepository>()));

        // Client service
        services.AddScoped<IClientService>(m => new ClientService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IAuthorizationClientRepository>(),
            m.GetRequiredService<IAuthorizationClientScopeRepository>(),
            m.GetRequiredService<IScopeRepository>(),
            m.GetRequiredService<IPasswordService>()));

        // Management services
        services.AddScoped<IScopeService>(m => new ScopeService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IScopeRepository>()));

        services.AddScoped<IRoleService>(m => new RoleService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IRoleRepository>(),
            m.GetRequiredService<IPermissionRepository>(),
            m.GetRequiredService<IRolePermissionRepository>()));

        services.AddScoped<IPermissionService>(m => new PermissionService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IPermissionRepository>()));

        services.AddScoped<IApiKeyService>(m => new ApiKeyService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IApiKeyRepository>(),
            m.GetRequiredService<IPasswordService>()));

        services.AddScoped<IProviderService>(m => new ProviderService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IProviderRepository>(),
            m.GetRequiredService<EnvironmentOptions>().AES_ENCRYPTION_KEY));

        services.AddScoped<IAgreementService>(m => new AgreementService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IAgreementRepository>(),
            m.GetRequiredService<IAgreementAcceptRepository>()));

        // Background service: periodically purges expired tokens to keep tables lean
        services.AddHostedService<TokenCleanupService>();

        // Webhook delivery pipeline (Phase 5-2)
        // WebhookChannel is the in-process bounded channel shared between the enqueue side
        // (IWebhookService) and the background delivery worker.
        services.AddSingleton<WebhookChannel>();
        services.AddScoped<IWebhookService>(m =>
            new WebhookService(m.GetRequiredService<WebhookChannel>()));
        services.AddHostedService<WebhookDeliveryBackgroundService>();
        
        #endregion

        return services;
    }

    public static IServiceCollection AddCustomDbContext(this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContextPool<AuthenticationDbContext>(options =>
        {
            options.UseNpgsql(connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(AuthenticationDbContext).GetTypeInfo().Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromMinutes(10), null);
                });
        });

        return services;
    }
}