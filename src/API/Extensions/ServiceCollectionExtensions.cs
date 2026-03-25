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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
            m.GetService<IHttpContextAccessor>(),
            m.GetService<ITenanttRepository>()));

        // Stateless utility services
        services.AddSingleton<IPasswordService, PasswordService>();

        services.AddScoped<IRefreshTokenProcessor>(m => new RefreshTokenProcessor(
            m.GetRequiredService<DomainIUnitOfWork>(),
            m.GetRequiredService<IRefreshTokenRepository>(),
            m.GetRequiredService<ITokenService>()));

        // Token service (scoped — one per request, reads RSA key from env)
        services.AddScoped<ITokenService>(m =>
        {
            var opts = m.GetRequiredService<EnvironmentOptions>();
            return new TokenService(
                m.GetRequiredService<DomainIUnitOfWork>(),
                m.GetRequiredService<IAccessTokenRepository>(),
                m.GetRequiredService<IRefreshTokenRepository>(),
                m.GetRequiredService<IAuthorizationCodeRepository>(),
                m.GetRequiredService<IRevokedTokenRepository>(),
                m.GetRequiredService<IUserRepository>(),
                opts.OAUTH2_SIGNED_PRIVATE_KEY,
                opts.AUTH_ISSUER,
                opts.TOKEN_EXPIRES_IN_MINUTES * 60);
        });

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
            m.GetRequiredService<IRefreshTokenProcessor>()));

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