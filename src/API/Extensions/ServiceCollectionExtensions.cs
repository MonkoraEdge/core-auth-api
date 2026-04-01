using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Services;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.Auth.Infrastructure.ExternalApis;
using MonkoraEdge.Core.Auth.Infrastructure.Configurations;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using MonkoraEdge.Core.Auth.Infrastructure.Extensions;
using MonkoraEdge.Core.Auth.Infrastructure.Services;
using MonkoraEdge.Core.Auth.Infrastructure.Services.Security;
using MonkoraEdge.Core.Auth.Infrastructure.Repositories;
using MonkoraEdge.Core.DotNet.Infrastructure;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Cryptography;
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
        
        services.AddScoped<ITenanttRepository, TenanttRepository>();

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

        // ApiKey aggregate
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        // Agreement aggregate
        services.AddScoped<IAgreementRepository, AgreementRepository>();
        services.AddScoped<IAgreementAcceptRepository, AgreementAcceptRepository>();

        #endregion


        #region Services     

        services.AddTransient<ITenantService>(m => new TenantService(
            m.GetService<DomainIUnitOfWork>(),
            m.GetService<ITenanttRepository>()));

        // Stateless utility services
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITwoFactorChallengeStore, MemoryTwoFactorChallengeStore>();

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

        // Token service (scoped — depends on scoped repositories)
        services.AddScoped<ITokenService>(m =>
        {
            var opts = m.GetRequiredService<EnvironmentOptions>();
            // OAUTH2_AUDIENCE separates the AS issuer from the resource server audience.
            // Falls back to AUTH_ISSUER for backward compatibility with deployments that
            // have not yet set this variable.
            var audience = !string.IsNullOrWhiteSpace(opts.OAUTH2_AUDIENCE)
                ? opts.OAUTH2_AUDIENCE
                : opts.AUTH_ISSUER;

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
                opts.TOKEN_EXPIRES_IN_MINUTES * 60);
        });

        // ── JWT Bearer authentication ──────────────────────────────────────────────────
        // Validates JWT access tokens on [Authorize] endpoints.
        // The RsaSecurityKey singleton is resolved via Configure<T> DI binding so the
        // validation key is always the same object that signs tokens — no double PEM load.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<RsaSecurityKey>((jwtOptions, rsaKey) =>
            {
                var audience = !string.IsNullOrWhiteSpace(options.OAUTH2_AUDIENCE)
                    ? options.OAUTH2_AUDIENCE
                    : options.AUTH_ISSUER;

                jwtOptions.MapInboundClaims = false; // keep claims as-is; do not remap to WS-Security URIs

                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey        = rsaKey,            // RS256 key from the singleton

                    // Restrict accepted algorithms — prevents alg=none and HS256 confusion attacks.
                    ValidAlgorithms         = new[] { SecurityAlgorithms.RsaSha256 },

                    // RFC 9068 §2.1: only accept tokens with typ=at+JWT; rejects ID tokens /
                    // generic JWTs from being used as access tokens on protected endpoints.
                    ValidTypes              = new[] { "at+JWT" },

                    ValidateIssuer          = true,
                    ValidIssuer             = options.AUTH_ISSUER,

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
                        var tokenHash = System.Convert.ToHexString(
                            System.Security.Cryptography.SHA256.HashData(
                                System.Text.Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

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
                m.GetRequiredService<IUserRoleRepository>(),
                m.GetRequiredService<IRoleRepository>(),
                m.GetRequiredService<IRefreshTokenRepository>(),
                m.GetRequiredService<ITokenService>(),
                m.GetRequiredService<IPasswordService>(),
                m.GetRequiredService<IAuthorizationClientRepository>(),
                m.GetRequiredService<ITwoFactorChallengeStore>(),
                opts.SIGNIN_FAILED_IN_MINUTES);
        });

        // Client authenticator — handles client verification and scope resolution
        services.AddScoped<IClientAuthenticator>(m => new ClientAuthenticator(
            m.GetRequiredService<IAuthorizationClientRepository>(),
            m.GetRequiredService<IAuthorizationClientScopeRepository>(),
            m.GetRequiredService<IScopeRepository>(),
            m.GetRequiredService<IPasswordService>()));

        // OAuth2 service
        services.AddScoped<IOAuth2Service>(m => new OAuth2Service(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IAuthorizationCodeRepository>(),
            m.GetRequiredService<IAuthorizationConsentRepository>(),
            m.GetRequiredService<IRefreshTokenRepository>(),
            m.GetRequiredService<IUserRepository>(),
            m.GetRequiredService<ITokenService>(),
            m.GetRequiredService<IClientAuthenticator>(),
            m.GetRequiredService<IPasswordService>(),
            m.GetRequiredService<IRefreshTokenProcessor>(),
            m.GetRequiredService<IAuditLogRepository>()));

        // User service
        services.AddScoped<IUserService>(m => new UserService(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IUserRepository>(),
            m.GetRequiredService<IUserIdentityRepository>(),
            m.GetRequiredService<IUserRoleRepository>(),
            m.GetRequiredService<IRoleRepository>(),
            m.GetRequiredService<IPasswordService>()));

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

        // Background service: periodically purges expired tokens to keep tables lean
        services.AddHostedService<TokenCleanupService>();
        
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