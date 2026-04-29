using Microsoft.Extensions.Caching.Distributed;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.SamlAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.Security.Encryption;
using System.Security.Cryptography;
using System.Text.Json;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services;

/// <summary>
/// Handles SAML 2.0 business logic: provider CRUD, JIT user provisioning, token issuance,
/// and the short-lived "saml_code" Redis handshake for browser-redirect flows.
/// HTTP binding (Saml2RedirectBinding / Saml2PostBinding) lives in the controller.
/// </summary>
public class SamlService : ISamlService
{
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISamlProviderRepository _samlProviderRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly ILoginAttemptRepository _loginAttemptRepo;
    private readonly ITokenService _tokenService;
    private readonly string _encryptionKey;

    private static readonly TimeSpan SamlCodeTtl = TimeSpan.FromMinutes(2);

    public SamlService(
        IDistributedCache cache,
        IUnitOfWork unitOfWork,
        ISamlProviderRepository samlProviderRepo,
        IUserRepository userRepo,
        IUserRoleRepository userRoleRepo,
        IRoleRepository roleRepo,
        ILoginAttemptRepository loginAttemptRepo,
        ITokenService tokenService,
        string encryptionKey)
    {
        _cache            = cache;
        _unitOfWork       = unitOfWork;
        _samlProviderRepo = samlProviderRepo;
        _userRepo         = userRepo;
        _userRoleRepo     = userRoleRepo;
        _roleRepo         = roleRepo;
        _loginAttemptRepo = loginAttemptRepo;
        _tokenService     = tokenService;
        _encryptionKey    = encryptionKey;
    }

    // ─── Provider lookup ──────────────────────────────────────────────────────

    public async Task<SamlProvider?> GetProviderAsync(string providerCode)
        => await _samlProviderRepo.GetByCodeAsync(providerCode);

    /// <summary>
    /// Returns a resolved POCO with the decrypted SP private key so the controller
    /// can build an <c>ITfoxtec.Identity.Saml2.Saml2Configuration</c> without
    /// accessing database or encryption logic directly.
    /// </summary>
    public Task<SamlSpConfig> GetSpConfigAsync(SamlProvider provider)
    {
        string? privateKeyPem = null;
        if (provider.SignAuthRequests && !string.IsNullOrEmpty(provider.SpPrivateKeyEncrypted))
            privateKeyPem = AesHelper.Decrypt(provider.SpPrivateKeyEncrypted, _encryptionKey);

        Dictionary<string, string>? attributeMapping = null;
        if (!string.IsNullOrEmpty(provider.AttributeMappingJson))
        {
            try
            {
                attributeMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    provider.AttributeMappingJson);
            }
            catch (JsonException) { /* ignore malformed mapping */ }
        }

        return Task.FromResult(new SamlSpConfig
        {
            ProviderId          = provider.Id,
            ProviderCode        = provider.ProviderCode,
            SpEntityId          = provider.SpEntityId,
            SpCertificatePem    = provider.SpCertificatePem,
            SpPrivateKeyPem     = privateKeyPem,
            SignAuthRequests     = provider.SignAuthRequests,
            WantAssertionsSigned = provider.WantAssertionsSigned,
            IdpEntityId          = provider.IdpEntityId,
            IdpSsoUrl            = provider.IdpSsoUrl,
            IdpSloUrl            = provider.IdpSloUrl,
            IdpCertificatePem    = provider.IdpCertificatePem,
            NameIdFormat         = provider.NameIdFormat,
            AutoProvisionUsers   = provider.AutoProvisionUsers,
            AttributeMapping     = attributeMapping
        });
    }

    // ─── Token issuance ──────────────────────────────────────────────────────

    public async Task<LoginResponse> IssueTokensAsync(
        SamlProvider provider,
        string nameIdentifier,
        string? email,
        string? firstName,
        string? lastName,
        string? ipAddress,
        string? userAgent)
    {
        // Use email claim preferentially; fall back to NameID (often an email for most IdPs).
        var effectiveEmail = (email ?? nameIdentifier).Trim().ToLowerInvariant();

        var user = await _userRepo.GetByEmailAsync(effectiveEmail);

        if (user == null)
        {
            if (!provider.AutoProvisionUsers)
                throw new DomainException("saml",
                    "No local account found for this SAML identity. Contact your administrator.");

            // JIT provisioning — create a local account linked to this SAML identity.
            var displayName = BuildDisplayName(firstName, lastName, effectiveEmail);
            user = new User
            {
                Email              = effectiveEmail,
                EmailVerified      = true,  // IdP has already verified
                DisplayName        = displayName,
                Status             = UserStatus.Active,
                RegistrationSource = "SAML",
                IsActive           = true
            };
            _userRepo.Insert(user);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            if (!user.IsActive || user.Status == UserStatus.Suspended)
                throw new DomainException("saml", "Account is disabled.");

            if (user.DeletedAt.HasValue)
                throw new DomainException("saml", "Account not found.");
        }

        return await IssueJwtTokensAsync(user, ipAddress, userAgent);
    }

    // ─── SAML code exchange ───────────────────────────────────────────────────

    public async Task<string> StoreSamlCodeAsync(LoginResponse loginResponse)
    {
        var code     = Guid.NewGuid().ToString("N");   // 32-char hex, URL-safe
        var cacheKey = $"saml:code:{code}";
        var json     = JsonSerializer.Serialize(loginResponse);

        await _cache.SetStringAsync(cacheKey, json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = SamlCodeTtl });

        return code;
    }

    public async Task<LoginResponse> ExchangeSamlCodeAsync(string samlCode)
    {
        // Validate input to prevent path traversal / cache key injection.
        if (string.IsNullOrWhiteSpace(samlCode) || samlCode.Length > 64 ||
            !samlCode.All(char.IsAsciiLetterOrDigit))
            throw new DomainException("saml", "Invalid SAML code.");

        var cacheKey = $"saml:code:{samlCode}";
        var json     = await _cache.GetStringAsync(cacheKey);
        if (json == null)
            throw new DomainException("saml", "SAML code not found or expired.");

        // One-time use — consume immediately.
        await _cache.RemoveAsync(cacheKey);

        return JsonSerializer.Deserialize<LoginResponse>(json)
            ?? throw new DomainException("saml", "Malformed SAML code payload.");
    }

    // ─── Admin CRUD ───────────────────────────────────────────────────────────

    public async Task<IEnumerable<SamlProvider>> GetAllProvidersAsync()
        => await _samlProviderRepo.GetAllActiveAsync();

    public async Task<SamlProvider> CreateProviderAsync(SamlProviderCreateRequest request)
    {
        var existing = await _samlProviderRepo.GetByCodeAsync(request.ProviderCode);
        if (existing != null)
            throw new DomainException("saml",
                $"Provider code '{request.ProviderCode}' is already in use.");

        var provider = new SamlProvider
        {
            ProviderCode        = request.ProviderCode.Trim().ToLowerInvariant(),
            DisplayName         = request.DisplayName,
            SpEntityId          = request.SpEntityId,
            SpCertificatePem    = request.SpCertificatePem,
            SpPrivateKeyEncrypted = EncryptPrivateKey(request.SpPrivateKeyPem),
            SignAuthRequests     = request.SignAuthRequests,
            WantAssertionsSigned = request.WantAssertionsSigned,
            IdpEntityId          = request.IdpEntityId,
            IdpSsoUrl            = request.IdpSsoUrl,
            IdpSloUrl            = request.IdpSloUrl,
            IdpCertificatePem    = request.IdpCertificatePem,
            IdpMetadataUrl       = request.IdpMetadataUrl,
            NameIdFormat         = request.NameIdFormat,
            AttributeMappingJson = request.AttributeMappingJson,
            AutoProvisionUsers   = request.AutoProvisionUsers,
            IsActive             = request.IsActive
        };

        _samlProviderRepo.Insert(provider);
        await _unitOfWork.SaveChangesAsync();
        return provider;
    }

    public async Task UpdateProviderAsync(Guid id, SamlProviderUpdateRequest request)
    {
        var provider = await _samlProviderRepo.GetByIdAsync(id)
            ?? throw new DomainException("saml", "Provider not found.");

        if (request.DisplayName    != null) provider.DisplayName    = request.DisplayName;
        if (request.SpEntityId     != null) provider.SpEntityId     = request.SpEntityId;
        if (request.SpCertificatePem != null) provider.SpCertificatePem = request.SpCertificatePem;
        if (request.SpPrivateKeyPem != null)
            provider.SpPrivateKeyEncrypted = EncryptPrivateKey(request.SpPrivateKeyPem);
        if (request.SignAuthRequests.HasValue)     provider.SignAuthRequests     = request.SignAuthRequests.Value;
        if (request.WantAssertionsSigned.HasValue) provider.WantAssertionsSigned = request.WantAssertionsSigned.Value;
        if (request.IdpEntityId    != null) provider.IdpEntityId    = request.IdpEntityId;
        if (request.IdpSsoUrl      != null) provider.IdpSsoUrl      = request.IdpSsoUrl;
        if (request.IdpSloUrl      != null) provider.IdpSloUrl      = request.IdpSloUrl;
        if (request.IdpCertificatePem != null) provider.IdpCertificatePem = request.IdpCertificatePem;
        if (request.IdpMetadataUrl != null) provider.IdpMetadataUrl = request.IdpMetadataUrl;
        if (request.NameIdFormat   != null) provider.NameIdFormat   = request.NameIdFormat;
        if (request.AttributeMappingJson != null) provider.AttributeMappingJson = request.AttributeMappingJson;
        if (request.AutoProvisionUsers.HasValue) provider.AutoProvisionUsers = request.AutoProvisionUsers.Value;
        if (request.IsActive.HasValue) provider.IsActive = request.IsActive.Value;

        _samlProviderRepo.Update(provider);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteProviderAsync(Guid id)
    {
        var provider = await _samlProviderRepo.GetByIdAsync(id)
            ?? throw new DomainException("saml", "Provider not found.");

        _samlProviderRepo.Delete(provider);
        await _unitOfWork.SaveChangesAsync();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<LoginResponse> IssueJwtTokensAsync(
        User user, string? ipAddress, string? userAgent)
    {
        var scopes    = new[] { "openid", "profile", "email" };
        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(null);

        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            Guid.Empty, user.Id, scopes, "saml", null, ipAddress, userAgent, expiresIn);

        var (refreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(
            Guid.Empty, user.Id, null, scopes,
            lifetimeSeconds: 30 * 24 * 60 * 60,  // 30-day refresh token
            ipAddress: ipAddress, userAgent: userAgent);

        user.RecordSuccessfulLogin();
        _userRepo.Update(user);

        _loginAttemptRepo.Insert(new LoginAttempt
        {
            UserId      = user.Id,
            Username    = user.Email,
            IpAddress   = ipAddress ?? "unknown",
            UserAgent   = userAgent,
            Success     = true,
            LoginMethod = "SAML"
        });

        await _unitOfWork.SaveChangesAsync();

        var userRoles  = await _userRoleRepo.GetByUserIdAsync(user.Id);
        var roleNames  = new List<string>();
        foreach (var ur in userRoles)
        {
            var role = await _roleRepo.GetByIdAsync(ur.RoleId);
            if (role != null) roleNames.Add(role.RoleCode);
        }

        return new LoginResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn    = expiresIn,
            User = new UserInfoResult
            {
                Id            = user.Id.ToString(),
                Email         = user.Email,
                DisplayName   = user.DisplayName,
                EmailVerified = user.EmailVerified,
                Status        = user.Status,
                Roles         = roleNames
            }
        };
    }

    private string? EncryptPrivateKey(string? plainPem)
    {
        if (string.IsNullOrEmpty(plainPem)) return null;
        return AesHelper.Encrypt(plainPem, _encryptionKey);
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string email)
    {
        if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
            return $"{firstName} {lastName}".Trim();
        return email;
    }
}
