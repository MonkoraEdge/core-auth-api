using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.PasskeyAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using System.Text.Json;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services;

/// <summary>
/// Implements the WebAuthn / FIDO2 passkey registration and authentication ceremonies
/// per W3C WebAuthn Level 3. Challenges are kept in Redis with a 5-minute TTL.
/// </summary>
public class PasskeyService : IPasskeyService
{
    private readonly Fido2 _fido2;
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasskeyCredentialRepository _credentialRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly ILoginAttemptRepository _loginAttemptRepo;
    private readonly ITokenService _tokenService;

    // Challenges are kept in Redis for a maximum of 5 minutes.
    private static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);

    public PasskeyService(
        Fido2 fido2,
        IDistributedCache cache,
        IUnitOfWork unitOfWork,
        IPasskeyCredentialRepository credentialRepo,
        IUserRepository userRepo,
        IUserRoleRepository userRoleRepo,
        IRoleRepository roleRepo,
        ILoginAttemptRepository loginAttemptRepo,
        ITokenService tokenService)
    {
        _fido2            = fido2;
        _cache            = cache;
        _unitOfWork       = unitOfWork;
        _credentialRepo   = credentialRepo;
        _userRepo         = userRepo;
        _userRoleRepo     = userRoleRepo;
        _roleRepo         = roleRepo;
        _loginAttemptRepo = loginAttemptRepo;
        _tokenService     = tokenService;
    }

    // ─── Registration ────────────────────────────────────────────────────────

    public async Task<string> BeginRegistrationAsync(Guid userId, string email, string? displayName)
    {
        var fido2User = new Fido2User
        {
            Id          = userId.ToByteArray(),
            Name        = email,
            DisplayName = displayName ?? email
        };

        // Exclude credentials the user already has so the authenticator won't offer
        // to overwrite an existing discoverable key for this RP.
        var existingKeys = (await _credentialRepo.GetByUserIdAsync(userId))
            .Where(c => c.IsActive)
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User               = fido2User,
            ExcludeCredentials = existingKeys,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                // Passkeys (discoverable credentials) — required for username-less sign-in.
                ResidentKey      = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required,
                // null = allow both platform and cross-platform authenticators
                AuthenticatorAttachment = null
            },
            AttestationPreference = AttestationConveyancePreference.None
        });

        var challengeId = Guid.NewGuid().ToString("N");
        var cacheKey    = $"fido2:reg:{challengeId}";

        await _cache.SetStringAsync(cacheKey, options.ToJson(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ChallengeTtl });

        // Return options as JSON; wrap with challengeId so the client echoes it back.
        var envelope = JsonSerializer.Serialize(new { challengeId, options = options.ToJson() });
        return envelope;
    }

    public async Task<PasskeyCredential> CompleteRegistrationAsync(
        Guid userId, string attestationResponseJson, string? friendlyName = null)
    {
        // Deserialize the envelope { challengeId, attestationResponse }
        using var doc = JsonDocument.Parse(attestationResponseJson);
        var root        = doc.RootElement;
        var challengeId = root.GetProperty("challengeId").GetString()
            ?? throw new DomainException("passkey", "Missing challengeId.");
        var attJson     = root.GetProperty("attestationResponse").GetRawText();

        var cacheKey = $"fido2:reg:{challengeId}";
        var optionsJson = await _cache.GetStringAsync(cacheKey)
            ?? throw new DomainException("passkey", "Registration session not found or expired.");

        var options  = CredentialCreateOptions.FromJson(optionsJson);
        var response = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(attJson)
            ?? throw new DomainException("passkey", "Invalid attestation response.");

        // Verify that the submitted userId matches the one embedded in the challenge.
        var challengeUserId = new Guid(options.User.Id);
        if (challengeUserId != userId)
            throw new DomainException("passkey", "User identity mismatch.");

        // MakeNewCredentialAsync validates the attestation and returns a verified credential.
        var result = await _fido2.MakeNewCredentialAsync(
            new MakeNewCredentialParams
            {
                AttestationResponse                = response,
                OriginalOptions                    = options,
                IsCredentialIdUniqueToUserCallback = async (args, ct) =>
                {
                    // Reject duplicate credential IDs across all users.
                    var base64Url = Base64UrlEncoder.Encode(args.CredentialId);
                    return await _credentialRepo.GetByCredentialIdAsync(base64Url) == null;
                }
            });

        // Consume the challenge so it cannot be replayed.
        await _cache.RemoveAsync(cacheKey);

        var credIdBase64Url = Base64UrlEncoder.Encode(result.Id);

        var credential = new PasskeyCredential
        {
            UserId                = userId,
            CredentialId          = result.Id,
            CredentialIdBase64Url = credIdBase64Url,
            PublicKey             = result.PublicKey,
            SignatureCounter      = result.SignCount,
            AaGuid                = result.AaGuid.ToString(),
            Transports            = result.Transports?.Select(t => t.ToString().ToLowerInvariant()).ToArray(),
            IsBackupEligible      = result.IsBackupEligible,
            IsBackedUp            = result.IsBackedUp,
            AttestationType       = result.AttestationFormat,
            FriendlyName          = friendlyName,
            IsActive              = true
        };

        _credentialRepo.Insert(credential);
        await _unitOfWork.SaveChangesAsync();

        return credential;
    }

    // ─── Authentication ──────────────────────────────────────────────────────

    public async Task<(string optionsJson, string challengeId)> BeginAuthenticationAsync(string? email)
    {
        List<PublicKeyCredentialDescriptor> allowedCredentials = [];

        if (!string.IsNullOrWhiteSpace(email))
        {
            var user = await _userRepo.GetByEmailAsync(email.Trim().ToLowerInvariant());
            if (user != null)
            {
                // Only include active credentials for this user.
                var creds = await _credentialRepo.GetByUserIdAsync(user.Id);
                allowedCredentials = creds
                    .Where(c => c.IsActive)
                    .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
                    .ToList();
            }
            // When email is given but not found, return empty allowedCredentials —
            // this prevents account enumeration through different response shapes.
        }

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowedCredentials,
            UserVerification   = UserVerificationRequirement.Required
        });

        var challengeId = Guid.NewGuid().ToString("N");
        var cacheKey    = $"fido2:auth:{challengeId}";

        await _cache.SetStringAsync(cacheKey, options.ToJson(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ChallengeTtl });

        return (options.ToJson(), challengeId);
    }

    public async Task<LoginResponse> CompleteAuthenticationAsync(
        string challengeId, string assertionResponseJson,
        string? ipAddress, string? userAgent)
    {
        var cacheKey    = $"fido2:auth:{challengeId}";
        var optionsJson = await _cache.GetStringAsync(cacheKey)
            ?? throw new DomainException("passkey", "Authentication session not found or expired.");

        var options  = AssertionOptions.FromJson(optionsJson);
        var response = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(assertionResponseJson)
            ?? throw new DomainException("passkey", "Invalid assertion response.");

        // Look up the stored credential by credential ID.
        var credIdBase64Url = Base64UrlEncoder.Encode(response.Id);
        var storedCred = await _credentialRepo.GetByCredentialIdAsync(credIdBase64Url)
            ?? throw new DomainException("passkey", "Credential not found.");

        if (!storedCred.IsActive)
            throw new DomainException("passkey", "Credential is disabled.");

        var result = await _fido2.MakeAssertionAsync(
            new MakeAssertionParams
            {
                AssertionResponse      = response,
                OriginalOptions        = options,
                StoredPublicKey        = storedCred.PublicKey,
                StoredSignatureCounter = storedCred.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
                {
                    // Verify the user handle in the assertion matches the credential owner.
                    if (args.UserHandle == null || args.UserHandle.Length == 0) return true;
                    var assertedUserId = new Guid(args.UserHandle);
                    return assertedUserId == storedCred.UserId;
                }
            });

        // Consume the challenge — prevents assertion replay.
        await _cache.RemoveAsync(cacheKey);

        // Alert-worthy if counter regressed — possible cloned credential.
        if (result.SignCount > 0 && result.SignCount <= storedCred.SignatureCounter)
            throw new DomainException("passkey",
                "Signature counter regression detected — possible cloned credential.");

        await _credentialRepo.UpdateSignatureCounterAsync(
            storedCred.Id, result.SignCount, DateTime.UtcNow);
        var user = await _userRepo.GetByIdAsync(storedCred.UserId)
            ?? throw new DomainException("passkey", "User not found.");

        if (!user.IsActive || user.Status == "SUSPENDED")
            throw new DomainException("passkey", "Account is disabled.");

        if (user.DeletedAt.HasValue)
            throw new DomainException("passkey", "Account not found.");

        return await IssueTokensAsync(user, ipAddress, userAgent);
    }

    // ─── Credential management ────────────────────────────────────────────────

    public async Task<IEnumerable<PasskeyCredential>> GetCredentialsAsync(Guid userId)
        => await _credentialRepo.GetByUserIdAsync(userId);

    public async Task DeleteCredentialAsync(Guid userId, Guid credentialId)
    {
        var cred = await _credentialRepo.GetByIdAsync(credentialId);
        if (cred == null || cred.UserId != userId)
            throw new DomainException("passkey", "Credential not found.");

        _credentialRepo.Delete(cred);
        await _unitOfWork.SaveChangesAsync();
    }

    // ─── Token issuing ────────────────────────────────────────────────────────

    private async Task<LoginResponse> IssueTokensAsync(
        User user, string? ipAddress, string? userAgent)
    {
        var scopes    = new[] { "openid", "profile", "email" };
        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(null);

        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            Guid.Empty, user.Id, scopes, "passkey", ipAddress, userAgent, expiresIn);

        var (refreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(
            Guid.Empty, user.Id, null, scopes,
            lifetimeSeconds: 30 * 24 * 60 * 60,   // 30-day refresh
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
            LoginMethod = "PASSKEY"
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
                Id           = user.Id.ToString(),
                Email        = user.Email,
                DisplayName  = user.DisplayName,
                EmailVerified = user.EmailVerified,
                Status       = user.Status,
                Roles        = roleNames
            }
        };
    }
}
