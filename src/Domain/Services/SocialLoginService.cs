using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.Security.Encryption;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MonkoraEdge.Core.Auth.Domain.Services;

/// <summary>
/// Implements the OAuth2/OIDC social login flow:
///   1. <see cref="InitiateAsync"/> — builds the authorization URL (with PKCE S256) and
///      caches the code_verifier in Redis so the callback handler can retrieve it by state.
///   2. <see cref="HandleCallbackAsync"/> — exchanges the authorization code for provider
///      tokens, resolves or creates a local user, links the external login record, and
///      issues local access + refresh tokens.
/// </summary>
public class SocialLoginService : ISocialLoginService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProviderRepository _providerRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserExternalLoginRepository _externalLoginRepo;
    private readonly ITokenService _tokenService;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _encryptionKey;
    private readonly ILogger<SocialLoginService> _logger;

    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);

    public SocialLoginService(
        IUnitOfWork unitOfWork,
        IProviderRepository providerRepo,
        IUserRepository userRepo,
        IUserExternalLoginRepository externalLoginRepo,
        ITokenService tokenService,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        string encryptionKey,
        ILogger<SocialLoginService> logger)
    {
        _unitOfWork = unitOfWork;
        _providerRepo = providerRepo;
        _userRepo = userRepo;
        _externalLoginRepo = externalLoginRepo;
        _tokenService = tokenService;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _encryptionKey = encryptionKey;
        _logger = logger;
    }

    // ─── ISocialLoginService ──────────────────────────────────────────────────

    public async Task<SocialLoginInitiateResponse> InitiateAsync(
        string providerCode, string? redirectUri, string? state, string? ipAddress)
    {
        var provider = await _providerRepo.GetByProviderCodeAsync(providerCode)
            ?? throw new DomainException("social", $"Provider '{providerCode}' not found.");

        if (!provider.IsActive)
            throw new DomainException("social", $"Provider '{providerCode}' is disabled.");

        if (string.IsNullOrEmpty(provider.AuthorizationUrl))
            throw new DomainException("social", $"Provider '{providerCode}' is missing AuthorizationUrl.");

        // Generate PKCE code_verifier + challenge (S256 only — OAuth2.1 §4.1.1)
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Generate or use caller-supplied state; embed codeVerifier in Redis under the state key.
        var stateKey = string.IsNullOrEmpty(state) ? GenerateState() : state;
        await _cache.SetStringAsync(
            SocialStateKey(stateKey),
            $"{codeVerifier}|{providerCode}",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = StateTtl });

        // Build the authorization URL.
        var callbackUrl = redirectUri ?? provider.CallbackUrl ?? string.Empty;
        var scopes = provider.Scopes.Length > 0
            ? string.Join(" ", provider.Scopes)
            : "openid profile email";

        var queryParams = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"]     = provider.ClientId ?? string.Empty,
            ["redirect_uri"]  = callbackUrl,
            ["scope"]         = scopes,
            ["state"]         = stateKey,
        };

        if (provider.PkceSupported)
        {
            queryParams["code_challenge"]        = codeChallenge;
            queryParams["code_challenge_method"] = "S256";
        }

        var qs = string.Join("&", queryParams.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return new SocialLoginInitiateResponse
        {
            AuthorizationUrl = $"{provider.AuthorizationUrl}?{qs}",
            State            = stateKey,
        };
    }

    public async Task<LoginResponse> HandleCallbackAsync(
        string providerCode, string code, string? state, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(state))
            throw new DomainException("social", "Missing state parameter.");

        // Retrieve and remove state entry from Redis.
        var stateValue = await _cache.GetStringAsync(SocialStateKey(state));
        if (string.IsNullOrEmpty(stateValue))
            throw new DomainException("social", "Invalid or expired state. Please retry the login.");

        await _cache.RemoveAsync(SocialStateKey(state));

        var parts = stateValue.Split('|', 2);
        var codeVerifier   = parts[0];
        var cachedProvider = parts.Length > 1 ? parts[1] : providerCode;

        if (!string.Equals(cachedProvider, providerCode, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("social", "Provider mismatch in state.");

        var provider = await _providerRepo.GetByProviderCodeAsync(providerCode)
            ?? throw new DomainException("social", $"Provider '{providerCode}' not found.");

        if (!provider.IsActive)
            throw new DomainException("social", $"Provider '{providerCode}' is disabled.");

        // Exchange code for tokens at the provider's token endpoint.
        var providerToken = await ExchangeCodeAsync(provider, code, codeVerifier);

        // Fetch the user's profile from the provider's UserInfo endpoint.
        var providerUser = await GetUserInfoAsync(provider, providerToken.AccessToken);

        if (string.IsNullOrEmpty(providerUser.Sub))
            throw new DomainException("social", "Social provider did not return a user identifier (sub).");

        // Upsert external login record and resolve/create local user.
        (User localUser, UserExternalLogin externalLogin) = await UpsertExternalLoginAsync(provider, providerUser, providerToken);

        // Issue local tokens (client_id = provider.Id, scopes = ["openid","profile","email"]).
        var scopes = new[] { "openid", "profile", "email" };
        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            provider.Id, localUser.Id, scopes, "social", ipAddress, userAgent);
        (string refreshRaw, Guid _rtId) = await _tokenService.GenerateRefreshTokenAsync(
            provider.Id, localUser.Id, null, scopes, lifetimeSeconds: 30 * 24 * 3600,
            ipAddress: ipAddress, userAgent: userAgent);
        var idToken = await _tokenService.GenerateIdTokenAsync(
            provider.ClientId ?? provider.Id.ToString(), localUser.Id, scopes, null, DateTime.UtcNow, accessToken);

        return new LoginResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshRaw,
            IdToken      = idToken,
            ExpiresIn    = _tokenService.GetAccessTokenLifetimeSeconds(),
            User = new UserInfoResult
            {
                Id            = localUser.Id.ToString(),
                Email         = localUser.Email,
                DisplayName   = localUser.DisplayName,
                EmailVerified = localUser.EmailVerified,
                Status        = localUser.Status,
            },
        };
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<ProviderTokenResponse> ExchangeCodeAsync(Provider provider, string code, string codeVerifier)
    {
        if (string.IsNullOrEmpty(provider.TokenUrl))
            throw new DomainException("social", $"Provider '{provider.ProviderCode}' is missing TokenUrl.");

        var clientSecret = string.Empty;
        if (!string.IsNullOrEmpty(provider.ClientSecretEncrypt))
            clientSecret = AesHelper.Decrypt(provider.ClientSecretEncrypt, _encryptionKey);

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["redirect_uri"]  = provider.CallbackUrl ?? string.Empty,
            ["client_id"]     = provider.ClientId ?? string.Empty,
            ["client_secret"] = clientSecret,
            ["code_verifier"] = codeVerifier,
        };

        var http = _httpClientFactory.CreateClient("SocialLogin");
        var response = await http.PostAsync(provider.TokenUrl, new FormUrlEncodedContent(form));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Social token exchange failed. Provider={Provider} Status={Status} Body={Body}",
                provider.ProviderCode, response.StatusCode, body);
            throw new DomainException("social", "Failed to exchange authorization code with external provider.");
        }

        return await response.Content.ReadFromJsonAsync<ProviderTokenResponse>()
            ?? throw new DomainException("social", "Invalid token response from external provider.");
    }

    private async Task<ProviderUserInfo> GetUserInfoAsync(Provider provider, string accessToken)
    {
        if (!string.IsNullOrEmpty(provider.UserinfoUrl))
        {
            var http = _httpClientFactory.CreateClient("SocialLogin");
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var userInfo = await http.GetFromJsonAsync<ProviderUserInfo>(provider.UserinfoUrl);
            if (userInfo != null) return userInfo;
        }

        // Fallback: decode JWT id_token claims (sub, email, name, picture)
        // This is a best-effort approach for providers that don't have a UserinfoUrl.
        throw new DomainException("social", $"Provider '{provider.ProviderCode}' has no UserinfoUrl configured.");
    }

    private async Task<(User localUser, UserExternalLogin externalLogin)> UpsertExternalLoginAsync(
        Provider provider, ProviderUserInfo providerUser, ProviderTokenResponse providerToken)
    {
        // Check if this external identity already exists.
        var externalLogin = await _externalLoginRepo.GetByProviderUserIdAsync(provider.Id, providerUser.Sub!);

        User localUser;

        if (externalLogin != null)
        {
            // Update stored tokens.
            externalLogin.ProviderDisplayName  = providerUser.Name;
            externalLogin.ProviderEmail        = providerUser.Email;
            externalLogin.AccessTokenEncrypt   = string.IsNullOrEmpty(providerToken.AccessToken)
                ? null : AesHelper.Encrypt(providerToken.AccessToken, _encryptionKey);
            externalLogin.RefreshTokenEncrypt  = string.IsNullOrEmpty(providerToken.RefreshToken)
                ? null : AesHelper.Encrypt(providerToken.RefreshToken, _encryptionKey);
            externalLogin.TokenExpiresAt       = providerToken.ExpiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(providerToken.ExpiresIn.Value) : null;
            externalLogin.UpdatedAt            = DateTime.UtcNow;

            localUser = await _userRepo.GetByIdAsync(externalLogin.UserId)
                ?? throw new DomainException("social", "Linked user account not found.");
        }
        else
        {
            // Try to match existing local user by email.
            localUser = (!string.IsNullOrEmpty(providerUser.Email)
                ? await _userRepo.GetByEmailAsync(providerUser.Email)
                : null) ?? CreateLocalUser(providerUser);

            if (localUser.Id == Guid.Empty)
                _userRepo.Insert(localUser);

            externalLogin = new UserExternalLogin
            {
                UserId               = localUser.Id,
                ProviderId           = provider.Id,
                ProviderUserId       = providerUser.Sub!,
                ProviderDisplayName  = providerUser.Name,
                ProviderEmail        = providerUser.Email,
                AccessTokenEncrypt   = string.IsNullOrEmpty(providerToken.AccessToken)
                    ? null : AesHelper.Encrypt(providerToken.AccessToken, _encryptionKey),
                RefreshTokenEncrypt  = string.IsNullOrEmpty(providerToken.RefreshToken)
                    ? null : AesHelper.Encrypt(providerToken.RefreshToken, _encryptionKey),
                TokenExpiresAt       = providerToken.ExpiresIn.HasValue
                    ? DateTime.UtcNow.AddSeconds(providerToken.ExpiresIn.Value) : null,
                Scopes               = providerToken.Scope?.Split(' '),
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow,
            };
            _externalLoginRepo.Insert(externalLogin);
        }

        await _unitOfWork.SaveChangesAsync();

        // Ensure localUser.Id is set after SaveChanges (in case it was a new entity).
        externalLogin.UserId = localUser.Id;
        return (localUser, externalLogin);
    }

    private static User CreateLocalUser(ProviderUserInfo providerUser) => new()
    {
        Email             = providerUser.Email ?? string.Empty,
        DisplayName       = providerUser.Name,
        EmailVerified     = !string.IsNullOrEmpty(providerUser.Email) && providerUser.EmailVerified,
        RegistrationSource = "SOCIAL",
        Status            = "ACTIVE",
        IsActive          = true,
        CreatedAt         = DateTime.UtcNow,
        UpdatedAt         = DateTime.UtcNow,
    };

    // ─── PKCE helpers ─────────────────────────────────────────────────────────

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string GenerateState()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string SocialStateKey(string state) => $"social:state:{state}";

    // ─── Internal DTOs ────────────────────────────────────────────────────────

    private sealed class ProviderTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private sealed class ProviderUserInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string? Email { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }
}
