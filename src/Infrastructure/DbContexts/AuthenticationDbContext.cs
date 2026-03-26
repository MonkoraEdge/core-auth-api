using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.Extensions;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Net;
using System.Diagnostics;
using System.Text.Json;
using BaseEntity = MonkoraEdge.Core.DotNet.Domain.SeedWork.BaseEntity;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts;

public class AuthenticationDbContext : DbContext
{
    // Master data
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserIdentity> UserIdentities { get; set; }
    public DbSet<Provider> Providers { get; set; }
    public DbSet<AuthorizationClient> AuthorizationClients { get; set; }
    public DbSet<Scope> Scopes { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<Agreement> Agreements { get; set; }

    // Transactions
    public DbSet<UserFile> UserFiles { get; set; }
    public DbSet<UserExternalLogin> UserExternalLogins { get; set; }
    public DbSet<UserSessionDevice> UserSessionDevices { get; set; }
    public DbSet<UserTwoFactorSetting> UserTwoFactorSettings { get; set; }
    public DbSet<UserTwoFactorRecoveryCode> UserTwoFactorRecoveryCodes { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<AccessToken> AccessTokens { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuthorizationConsent> AuthorizationConsents { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<RateLimit> RateLimits { get; set; }
    public DbSet<PasswordHistory> PasswordHistories { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<RevokedToken> RevokedTokens { get; set; }
    public DbSet<AgreementAccept> AgreementAccepts { get; set; }

    // Audit
    public DbSet<AuditLog> AuditLogs { get; set; }

    // Links
    public DbSet<AuthorizationClientScope> AuthorizationClientScopes { get; set; }
    public DbSet<AuthorizationCodeScope> AuthorizationCodeScopes { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : base(options)
    {
        Debug.WriteLine("AuthenticationDbContext::ctor ->" + GetHashCode());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Locale>()
            .HaveConversion<LocaleJsonValueConverter>()
            .HaveColumnType("jsonb");

        configurationBuilder.Properties<TenantSettings>()
            .HaveConversion<TenantSettingsJsonValueConverter>()
            .HaveColumnType("jsonb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.ApplyConfiguration(new TenantEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserFileEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserIdentityEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProviderEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserExternalLoginEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserSessionDeviceEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserTwoFactorSettingEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserTwoFactorRecoveryCodeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationClientEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserSessionEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ScopeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationClientScopeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationCodeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationCodeScopeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AccessTokenEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationConsentEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new LoginAttemptEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RateLimitEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordHistoryEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new EmailVerificationEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordResetEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RevokedTokenEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ApiKeyEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AgreementEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AgreementAcceptEntityTypeConfiguration());

        modelBuilder.ApplyGlobalFiltersSoftDeleted();
        modelBuilder.UseSnakeCaseNames(DatabaseType.PostgreSql);
        ApplySharedColumnConventions(modelBuilder);
        ApplyBaseEntityDefaults(modelBuilder);
    }

    private static void ApplySharedColumnConventions(ModelBuilder modelBuilder)
    {
        var ipAddressConverter = new ValueConverter<string?, IPAddress?>(
            value => string.IsNullOrWhiteSpace(value) ? null : IPAddress.Parse(value),
            value => value == null ? null : value.ToString());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var entityBuilder = modelBuilder.Entity(entityType.ClrType);

            var descriptionProperty = entityType.FindProperty(nameof(BaseEntity.Description));
            if (descriptionProperty?.ClrType == typeof(Locale))
            {
                entityBuilder.Property(typeof(Locale), nameof(BaseEntity.Description)).HasColumnType("jsonb");
            }

            var ipAddressProperty = entityType.FindProperty("IpAddress");
            if (ipAddressProperty?.ClrType == typeof(string))
            {
                entityBuilder.Property<string>("IpAddress")
                    .HasConversion(ipAddressConverter)
                    .HasColumnType("inet");
            }
        }
    }

    private static void ApplyBaseEntityDefaults(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty(nameof(BaseEntity.Id));
            if (idProperty?.ClrType == typeof(Guid))
            {
                idProperty.SetDefaultValueSql("gen_random_uuid()");
            }

            entityType.FindProperty(nameof(BaseEntity.CreatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
            entityType.FindProperty(nameof(BaseEntity.CreatedBy))?.SetDefaultValueSql("'SYSTEM'");
            entityType.FindProperty(nameof(BaseEntity.UpdatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
            entityType.FindProperty(nameof(BaseEntity.UpdatedBy))?.SetDefaultValueSql("'SYSTEM'");
        }
    }
}

file sealed class LocaleJsonValueConverter() : ValueConverter<Locale, string>(
    value => JsonValueConverterHelpers.Serialize(value),
    value => JsonValueConverterHelpers.Deserialize<Locale>(value))
{
}

file sealed class TenantSettingsJsonValueConverter() : ValueConverter<TenantSettings, string>(
    value => JsonValueConverterHelpers.Serialize(value),
    value => JsonValueConverterHelpers.Deserialize<TenantSettings>(value))
{
}

file static class JsonValueConverterHelpers
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize<TValue>(TValue value)
        where TValue : class
        => JsonSerializer.Serialize(value, SerializerOptions);

    public static TValue Deserialize<TValue>(string value)
        where TValue : class, new()
        => string.IsNullOrWhiteSpace(value)
            ? new TValue()
            : JsonSerializer.Deserialize<TValue>(value, SerializerOptions) ?? new TValue();
}