using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepo;
    private readonly IUserIdentityRepository _identityRepo;
    private readonly IUserTwoFactorSettingRepository _twoFactorRepo;
    private readonly IUserTwoFactorRecoveryCodeRepository _recoveryCodeRepo;
    private readonly IEmailVerificationRepository _emailVerifRepo;
    private readonly IPasswordResetRepository _passwordResetRepo;
    private readonly IPasswordHistoryRepository _passwordHistoryRepo;
    private readonly ILoginAttemptRepository _loginAttemptRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly IAuthorizationClientRepository _clientRepo;
    private readonly ITwoFactorChallengeStore _twoFactorChallengeStore;
    private readonly INotificationApi _notificationApi;
    private readonly int _signinFailedMinutes;

    // How long a 2FA challenge token is valid after the password step.
    private static readonly TimeSpan TwoFactorChallengeExpiry = TimeSpan.FromMinutes(5);

    public AuthService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepo,
        IUserIdentityRepository identityRepo,
        IUserTwoFactorSettingRepository twoFactorRepo,
        IUserTwoFactorRecoveryCodeRepository recoveryCodeRepo,
        IEmailVerificationRepository emailVerifRepo,
        IPasswordResetRepository passwordResetRepo,
        IPasswordHistoryRepository passwordHistoryRepo,
        ILoginAttemptRepository loginAttemptRepo,
        IUserRoleRepository userRoleRepo,
        IRoleRepository roleRepo,
        IRefreshTokenRepository refreshTokenRepo,
        ITokenService tokenService,
        IPasswordService passwordService,
        IAuthorizationClientRepository clientRepo,
        ITwoFactorChallengeStore twoFactorChallengeStore,
        INotificationApi notificationApi,
        int signinFailedMinutes = 15)
    {
        _unitOfWork = unitOfWork;
        _userRepo = userRepo;
        _identityRepo = identityRepo;
        _twoFactorRepo = twoFactorRepo;
        _recoveryCodeRepo = recoveryCodeRepo;
        _emailVerifRepo = emailVerifRepo;
        _passwordResetRepo = passwordResetRepo;
        _passwordHistoryRepo = passwordHistoryRepo;
        _loginAttemptRepo = loginAttemptRepo;
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _clientRepo = clientRepo;
        _twoFactorChallengeStore = twoFactorChallengeStore;
        _notificationApi = notificationApi;
        _signinFailedMinutes = signinFailedMinutes;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        // Rate-limit by IP
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var since = DateTime.UtcNow.AddMinutes(-_signinFailedMinutes);
            var failCount = await _loginAttemptRepo.CountFailedByIpAddressAsync(ipAddress, since);
            if (failCount >= 10)
                throw new DomainException("login", "Too many failed attempts from this IP. Try again later.");
        }

        // Rate-limit by username (prevents targeted account lockout farming)
        var sinceByName = DateTime.UtcNow.AddMinutes(-_signinFailedMinutes);
        var failCountByName = await _loginAttemptRepo.CountFailedByUsernameAsync(request.Username, sinceByName);
        if (failCountByName >= 10)
            throw new DomainException("login", "Too many failed attempts for this account. Try again later.");

        var user = await _userRepo.GetByEmailAsync(request.Username);
        if (user == null)
        {
            // Perform dummy bcrypt verify to normalise response time and prevent username enumeration
            _passwordService.PerformDummyVerify(request.Password);
            await RecordLoginAttemptAsync(null, request.Username, null, ipAddress, userAgent, false, "USER_NOT_FOUND", request.ClientId);
            await _unitOfWork.SaveChangesAsync();
            throw new DomainException("login", "Invalid username or password.");
        }

        var identity = await _identityRepo.GetByUserIdAsync(user.Id);
        if (identity == null || string.IsNullOrEmpty(identity.PasswordHash))
        {
            _passwordService.PerformDummyVerify(request.Password);
            await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "USER_NOT_FOUND", request.ClientId);
            await _unitOfWork.SaveChangesAsync();
            throw new DomainException("login", "Invalid username or password.");
        }

        if (identity.IsLocked)
        {
            await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "ACCOUNT_LOCKED", request.ClientId);
            await _unitOfWork.SaveChangesAsync();
            throw new DomainException("login", $"Account is locked until {identity.LockedUntil:u}.");
        }

        if (!user.IsActive || user.Status == "SUSPENDED")
        {
            await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "ACCOUNT_DISABLED", request.ClientId);
            await _unitOfWork.SaveChangesAsync();
            throw new DomainException("login", "Account is disabled.");
        }

        if (!_passwordService.VerifyPassword(request.Password, identity.PasswordHash))
        {
            identity.RecordFailedAttempt(5, _signinFailedMinutes);
            _identityRepo.Update(identity);

            await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "INVALID_PASSWORD", request.ClientId);
            await _unitOfWork.SaveChangesAsync();
            throw new DomainException("login", "Invalid username or password.");
        }

        identity.ClearLock();
        _identityRepo.Update(identity);

        // Check 2FA requirement
        var twoFactorSettings = await _twoFactorRepo.GetByUserIdAsync(user.Id);
        var activeTwoFactor = twoFactorSettings.FirstOrDefault(t => t.IsActive);

        if (activeTwoFactor != null)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode) && string.IsNullOrEmpty(request.TwoFactorRecoveryCode))
            {
                user.LastActivityAt = DateTime.UtcNow;
                _userRepo.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // Store the challenge token so VerifyTwoFactorLoginAsync can resolve the userId
                // without trusting a client-supplied identifier.
                var challengeToken = _passwordService.GenerateSecureToken(32);
                await _twoFactorChallengeStore.StoreAsync(challengeToken, user.Id, request.ClientId, TwoFactorChallengeExpiry);

                return new LoginResponse
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = challengeToken
                };
            }

            bool twoFactorValid = false;

            if (!string.IsNullOrEmpty(request.TwoFactorCode))
            {
                twoFactorValid = activeTwoFactor.DeviceType == "TOTP"
                    && _passwordService.VerifyTotpCode(activeTwoFactor.SecretKey!, request.TwoFactorCode);
            }
            else if (!string.IsNullOrEmpty(request.TwoFactorRecoveryCode))
            {
                // Strip formatting hyphens before hashing — storage normalizes the same way in EnableTwoFactorAsync.
                var codeHash = _passwordService.HashSha256(request.TwoFactorRecoveryCode.Replace("-", "").ToUpperInvariant());
                var recoveryCode = await _recoveryCodeRepo.GetActiveByCodeHashAsync(codeHash);
                if (recoveryCode != null)
                {
                    twoFactorValid = true;
                    recoveryCode.UsedAt = DateTime.UtcNow;
                    recoveryCode.IsActive = false;
                    _recoveryCodeRepo.Update(recoveryCode);
                }
            }

            if (!twoFactorValid)
            {
                await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "INVALID_OTP", request.ClientId);
                await _unitOfWork.SaveChangesAsync();
                throw new DomainException("login", "Invalid two-factor authentication code.");
            }
        }

        return await IssueTokensAndFinalizeLoginAsync(user, request.ClientId, ipAddress, userAgent);
    }

    public async Task<CreateResponse> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        if (!_passwordService.MeetsPasswordPolicy(request.Password))
            throw new DomainException("register", "Password does not meet policy requirements (min 8 chars, upper, lower, digit, special).");

        var existingUser = await _userRepo.GetByEmailAsync(request.Email.ToLowerInvariant());
        if (existingUser != null)
            throw new DomainException("register", "Email address is already registered.");

        if (!string.IsNullOrEmpty(request.PhoneNumber) && !System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^\+[1-9]\d{1,14}$"))
            throw new DomainException("register", "Phone number must be in E.164 format (e.g., +66812345678).");

        var user = new User
        {
            TenantId = request.TenantId,
            Email = request.Email.ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber,
            DisplayName = request.DisplayName ?? request.Email,
            LocaleCode = request.Locale ?? "en",
            Status = "ACTIVE",
            RegistrationSource = "LOCAL",
            IsActive = true,
            EmailVerified = false
        };
        _userRepo.Insert(user);

        var identity = new UserIdentity
        {
            UserId = user.Id,
            ProviderType = "LOCAL",
            Username = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            PasswordAlgo = "BCRYPT",
            PasswordUpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _identityRepo.Insert(identity);

        await _unitOfWork.SaveChangesAsync();
        await SendVerificationEmailAsync(user.Id, user.Email);

        return new CreateResponse { Id = user.Id, IsSuccess = true, Message = "Registration successful. Please verify your email." };
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken, bool allDevices, string? ipAddress)
    {
        if (allDevices || string.IsNullOrEmpty(refreshToken))
        {
            await _tokenService.RevokeAllUserTokensAsync(userId, reason: "logout");
        }
        else
        {
            var rt = await _refreshTokenRepo.GetByTokenHashAsync(_tokenService.HashToken(refreshToken));
            if (rt != null)
            {
                // Revoke the full rotation chain and cascade to active access tokens so
                // that any token in the family cannot be replayed after explicit logout.
                if (rt.FamilyId != Guid.Empty)
                    await _tokenService.RevokeTokenFamilyAsync(rt.FamilyId, "logout");
                else
                    await _tokenService.RevokeTokenAsync(refreshToken, "refresh_token", rt.ClientId, "logout");
            }
        }

        var user = await _userRepo.GetByIdAsync(userId);
        if (user != null)
        {
            user.LastActivityAt = DateTime.UtcNow;
            _userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email.ToLowerInvariant());
        if (user == null) return; // Security: do not reveal if email exists

        var rawToken = _passwordService.GenerateSecureToken(48);
        var tokenHash = _passwordService.HashSha256(rawToken);

        _passwordResetRepo.Insert(new PasswordReset
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await _unitOfWork.SaveChangesAsync();
        await _notificationApi.SendPasswordResetAsync(user.Email, rawToken, user.DisplayName);
    }

    public async Task<UpdateResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (!_passwordService.MeetsPasswordPolicy(request.NewPassword))
            throw new DomainException("reset_password", "Password does not meet policy requirements.");

        var tokenHash = _passwordService.HashSha256(request.Token);
        var reset = await _passwordResetRepo.GetByTokenHashAsync(tokenHash);

        if (reset == null || reset.ExpiresAt <= DateTime.UtcNow || reset.UsedAt.HasValue)
            throw new DomainException("reset_password", "Reset token is invalid or has expired.");

        var user = await _userRepo.GetByIdAsync(reset.UserId);
        if (user == null || !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("reset_password", "Invalid request.");

        var identity = await _identityRepo.GetByUserIdAsync(user.Id);
        if (identity == null)
            throw new DomainException("reset_password", "User identity not found.");

        var history = await _passwordHistoryRepo.GetByUserIdAsync(user.Id, 5);
        if (history.Any(h => _passwordService.VerifyPassword(request.NewPassword, h.PasswordHash)))
            throw new DomainException("reset_password", "Cannot reuse a recent password.");

        // Atomically mark the token as used — guards against concurrent replay
        var consumed = await _passwordResetRepo.TryConsumeAsync(reset.Id, DateTime.UtcNow);
        if (!consumed)
            throw new DomainException("reset_password", "Reset token is invalid or has expired.");

        var oldHash = identity.PasswordHash ?? string.Empty;
        identity.SetPassword(_passwordService.HashPassword(request.NewPassword));
        identity.ClearLock();
        _identityRepo.Update(identity);

        _passwordHistoryRepo.Insert(new PasswordHistory { UserId = user.Id, PasswordHash = oldHash });

        user.LastPasswordChangedAt = DateTime.UtcNow;
        _userRepo.Update(user);

        await _tokenService.RevokeAllUserTokensAsync(user.Id, reason: "password_reset");
        await _unitOfWork.SaveChangesAsync();

        return new UpdateResponse { Id = user.Id, IsSuccess = true, Message = "Password has been reset." };
    }

    public async Task<UpdateResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        if (!_passwordService.MeetsPasswordPolicy(request.NewPassword))
            throw new DomainException("change_password", "Password does not meet policy requirements.");

        var identity = await _identityRepo.GetByUserIdAsync(userId);
        if (identity == null || string.IsNullOrEmpty(identity.PasswordHash))
            throw new DomainException("change_password", "Identity not found.");

        if (!_passwordService.VerifyPassword(request.CurrentPassword, identity.PasswordHash))
            throw new DomainException("change_password", "Current password is incorrect.");

        var history = await _passwordHistoryRepo.GetByUserIdAsync(userId, 5);
        if (history.Any(h => _passwordService.VerifyPassword(request.NewPassword, h.PasswordHash)))
            throw new DomainException("change_password", "Cannot reuse a recent password.");

        var oldHash = identity.PasswordHash;
        identity.SetPassword(_passwordService.HashPassword(request.NewPassword));
        _identityRepo.Update(identity);

        _passwordHistoryRepo.Insert(new PasswordHistory { UserId = userId, PasswordHash = oldHash });

        var user = await _userRepo.GetByIdAsync(userId);
        if (user != null) { user.LastPasswordChangedAt = DateTime.UtcNow; _userRepo.Update(user); }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = userId, IsSuccess = true, Message = "Password changed." };
    }

    public async Task SendVerificationEmailAsync(Guid userId, string email)
    {
        // Cooldown: prevent email spam by rejecting resend requests within a 60-second window.
        var pending = (await _emailVerifRepo.GetByUserIdAsync(userId))
            .Where(v => v.VerifiedAt == null && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();
        if (pending != null && (DateTime.UtcNow - pending.CreatedAt).TotalSeconds < 60)
            throw new DomainException("resend_verification", "Please wait 60 seconds before requesting another verification email.");

        // Invalidate any still-pending tokens so previous links cannot be reused after resend
        await _emailVerifRepo.InvalidatePendingByUserIdAsync(userId);

        var rawToken = _passwordService.GenerateSecureToken(48);
        var tokenHash = _passwordService.HashSha256(rawToken);

        _emailVerifRepo.Insert(new EmailVerification
        {
            UserId = userId,
            Email = email,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
        await _unitOfWork.SaveChangesAsync();
        await _notificationApi.SendEmailVerificationAsync(email, rawToken);
    }

    public async Task<UpdateResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var tokenHash = _passwordService.HashSha256(request.Token);
        var verif = await _emailVerifRepo.GetByTokenHashAsync(tokenHash);

        if (verif == null || verif.ExpiresAt <= DateTime.UtcNow || verif.VerifiedAt.HasValue)
            throw new DomainException("verify_email", "Token is invalid or has expired.");

        if (!string.Equals(verif.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("verify_email", "Email does not match.");

        // Atomically mark as verified — guards against concurrent double-verification
        var verified = await _emailVerifRepo.TryMarkVerifiedAsync(verif.Id, DateTime.UtcNow);
        if (!verified)
            throw new DomainException("verify_email", "Token is invalid or has expired.");

        var user = await _userRepo.GetByIdAsync(verif.UserId);
        if (user != null)
        {
            user.MarkEmailVerified();
            _userRepo.Update(user);
        }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = verif.UserId, IsSuccess = true, Message = "Email verified." };
    }

    public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(Guid userId, TwoFactorSetupRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) throw new DomainException("2fa_setup", "User not found.");

        if (request.DeviceType == "SMS" || request.DeviceType == "EMAIL")
            throw new DomainException("2fa_setup", $"2FA via {request.DeviceType} is not supported in this deployment. Please use TOTP.");

        var response = new TwoFactorSetupResponse { DeviceType = request.DeviceType };

        if (request.DeviceType == "TOTP")
        {
            // Deactivate any existing unverified TOTP setting before creating a new one
            var existing = await _twoFactorRepo.GetByUserIdAndDeviceTypeAsync(userId, "TOTP");
            if (existing != null && !existing.IsActive)
            {
                existing.DeletedAt = DateTime.UtcNow;
                _twoFactorRepo.Update(existing);
            }

            var secret = _passwordService.GenerateTotpSecret();
            var uri = _passwordService.GetTotpUri(secret, user.Email, _tokenService.GetIssuer());
            response.TotpUri = uri;

            _twoFactorRepo.Insert(new UserTwoFactorSetting
            {
                UserId = userId,
                DeviceType = "TOTP",
                SecretKey = secret,
                IsActive = false
            });
            await _unitOfWork.SaveChangesAsync();
        }

        return response;
    }

    public async Task<TwoFactorEnableResponse> EnableTwoFactorAsync(Guid userId, TwoFactorVerifyRequest request)
    {
        var settings = await _twoFactorRepo.GetByUserIdAsync(userId);
        var pending = settings.FirstOrDefault(s => s.DeviceType == request.DeviceType && !s.IsActive);
        if (pending == null)
            throw new DomainException("2fa_enable", "No pending 2FA setup found for this device type.");

        bool valid = request.DeviceType == "TOTP"
            && _passwordService.VerifyTotpCode(pending.SecretKey!, request.Code);

        if (!valid)
            throw new DomainException("2fa_enable", "Invalid verification code.");

        pending.IsActive = true;
        pending.VerifiedAt = DateTime.UtcNow;
        _twoFactorRepo.Update(pending);

        // Persist recovery codes
        var batchId = Guid.NewGuid();
        var codes = _passwordService.GenerateRecoveryCodes(10);
        foreach (var code in codes)
        {
            var normalized = code.Replace("-", "").ToUpperInvariant();
            _recoveryCodeRepo.Insert(new UserTwoFactorRecoveryCode
            {
                UserId = userId,
                TwoFactorSettingId = pending.Id,
                BatchId = batchId,
                CodeHash = _passwordService.HashSha256(normalized),
                CodePrefix = code.Split('-')[0],
                IsActive = true
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return new TwoFactorEnableResponse { IsSuccess = true, RecoveryCodes = codes };
    }

    public async Task<UpdateResponse> DisableTwoFactorAsync(Guid userId, TwoFactorDisableRequest request)
    {
        var identity = await _identityRepo.GetByUserIdAsync(userId);
        if (identity == null || !_passwordService.VerifyPassword(request.Password, identity.PasswordHash ?? ""))
            throw new DomainException("2fa_disable", "Invalid password.");

        var settings = await _twoFactorRepo.GetByUserIdAsync(userId);
        foreach (var s in settings.Where(s => s.IsActive))
        {
            s.IsActive = false;
            _twoFactorRepo.Update(s);
        }

        // Invalidate all active recovery codes so they cannot be replayed if 2FA is re-enabled later.
        var activeCodes = await _recoveryCodeRepo.GetActiveByUserIdAsync(userId);
        foreach (var rc in activeCodes)
        {
            rc.IsActive = false;
            _recoveryCodeRepo.Update(rc);
        }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = userId, IsSuccess = true, Message = "Two-factor authentication disabled." };
    }

    public async Task<LoginResponse> VerifyTwoFactorLoginAsync(
        string twoFactorToken, string code, string deviceType, string? ipAddress, string? userAgent)
    {
        // Validate the opaque token issued during the password-correct + 2FA-required step.
        // Consuming removes it — tokens are single-use with a 5-minute TTL.
        var challenge = await _twoFactorChallengeStore.ConsumeAsync(twoFactorToken)
            ?? throw new DomainException("2fa_verify", "2FA session has expired or is invalid. Please sign in again.");

        var settings = await _twoFactorRepo.GetByUserIdAsync(challenge.UserId);
        var setting = settings.FirstOrDefault(s => s.DeviceType == deviceType && s.IsActive);
        if (setting == null)
            throw new DomainException("2fa_verify", "2FA is not configured for this device type.");

        bool valid = deviceType == "TOTP"
            && _passwordService.VerifyTotpCode(setting.SecretKey!, code);

        if (!valid)
            throw new DomainException("2fa_verify", "Invalid 2FA code.");

        var user = await _userRepo.GetByIdAsync(challenge.UserId);
        if (user == null) throw new DomainException("2fa_verify", "User not found.");

        return await IssueTokensAndFinalizeLoginAsync(user, challenge.ClientId, ipAddress, userAgent);
    }

    // ─── Private helpers ────────────────────────────────────────────────────────

    private async Task<LoginResponse> IssueTokensAndFinalizeLoginAsync(
        User user, string? clientId, string? ipAddress, string? userAgent)
    {
        Guid resolvedClientId = Guid.Empty;
        int accessLifetime = 3600;
        int refreshLifetime = 2592000;

        if (!string.IsNullOrEmpty(clientId))
        {
            var client = await _clientRepo.GetByClientIdAsync(clientId);
            if (client != null)
            {
                resolvedClientId = client.Id;
                accessLifetime = client.AccessTokenLifetime;
                refreshLifetime = client.RefreshTokenLifetime;
            }
        }

        var scopes = new[] { "openid", "profile", "email" };

        // RFC 6749 §5.1: ExpiresIn MUST reflect the actual JWT lifetime.
        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(accessLifetime);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            resolvedClientId, user.Id, scopes, "direct", ipAddress, userAgent, expiresIn);
        var (refreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(
            resolvedClientId, user.Id, null, scopes, refreshLifetime,
            ipAddress: ipAddress, userAgent: userAgent);

        user.RecordSuccessfulLogin();
        _userRepo.Update(user);

        await RecordLoginAttemptAsync(user.Id, user.Email,
            resolvedClientId == Guid.Empty ? null : resolvedClientId,
            ipAddress, userAgent, true, null, clientId);
        await _unitOfWork.SaveChangesAsync();

        var userRoles = await _userRoleRepo.GetByUserIdAsync(user.Id);
        var roleNames = new List<string>();
        foreach (var ur in userRoles)
        {
            var role = await _roleRepo.GetByIdAsync(ur.RoleId);
            if (role != null) roleNames.Add(role.RoleCode);
        }

        return new LoginResponse
        {
            RequiresTwoFactor = false,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            User = new UserInfoResult
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                DisplayName = user.DisplayName,
                EmailVerified = user.EmailVerified,
                Status = user.Status,
                Roles = roleNames
            }
        };
    }

    private async Task RecordLoginAttemptAsync(
        Guid? userId, string? username, Guid? clientId,
        string? ipAddress, string? userAgent, bool success,
        string? failureReason, string? clientIdStr)
    {
        Guid resolvedClientId = Guid.Empty;
        if (clientId.HasValue)
            resolvedClientId = clientId.Value;
        else if (!string.IsNullOrEmpty(clientIdStr))
        {
            var client = await _clientRepo.GetByClientIdAsync(clientIdStr);
            if (client != null) resolvedClientId = client.Id;
        }

        _loginAttemptRepo.Insert(new LoginAttempt
        {
            ClientId = resolvedClientId,
            UserId = userId,
            Username = username,
            IpAddress = ipAddress ?? "unknown",
            UserAgent = userAgent,
            Success = success,
            FailureReason = failureReason,
            LoginMethod = "LOCAL"
        });
    }

    // ─── Magic Link ────────────────────────────────────────────────────────────

    public async Task SendMagicLinkAsync(string email, string? clientId, string? redirectUri, string? ipAddress)
    {
        // Normalise email
        email = email.Trim().ToLowerInvariant();

        // Silently succeed for unknown emails — prevents account enumeration.
        var user = await _userRepo.GetByEmailAsync(email);
        if (user == null || user.DeletedAt.HasValue) return;

        // Cooldown: at most one magic-link per 60 seconds per user.
        var pending = (await _emailVerifRepo.GetByUserIdAsync(user.Id))
            .Where(v => v.VerificationType == "MAGIC_LINK" &&
                        v.VerifiedAt == null &&
                        v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();
        if (pending != null && (DateTime.UtcNow - pending.CreatedAt).TotalSeconds < 60)
            return; // silently swallow — no enumeration leak

        // Invalidate stale magic-link tokens before issuing a fresh one.
        await _emailVerifRepo.InvalidatePendingByUserIdAsync(user.Id);

        var rawToken = _passwordService.GenerateSecureToken(48);
        var tokenHash = _passwordService.HashSha256(rawToken);

        _emailVerifRepo.Insert(new EmailVerification
        {
            UserId           = user.Id,
            Email            = email,
            VerificationType = "MAGIC_LINK",
            TokenHash        = tokenHash,
            // Magic links expire in 15 minutes — short enough to be secure.
            ExpiresAt        = DateTime.UtcNow.AddMinutes(15)
        });
        await _unitOfWork.SaveChangesAsync();

        await _notificationApi.SendMagicLinkAsync(email, rawToken, clientId, redirectUri);
    }

    public async Task<LoginResponse> VerifyMagicLinkAsync(string token, string? ipAddress, string? userAgent)
    {
        var tokenHash = _passwordService.HashSha256(token.Trim());
        var verif = await _emailVerifRepo.GetByTokenHashAsync(tokenHash);

        if (verif == null
            || verif.VerificationType != "MAGIC_LINK"
            || verif.ExpiresAt <= DateTime.UtcNow
            || verif.VerifiedAt.HasValue)
            throw new DomainException("magic_link", "Magic link is invalid or has expired.");

        // Atomically consume the token — single-use guard
        var consumed = await _emailVerifRepo.TryMarkVerifiedAsync(verif.Id, DateTime.UtcNow);
        if (!consumed)
            throw new DomainException("magic_link", "Magic link has already been used.");

        var user = await _userRepo.GetByIdAsync(verif.UserId);
        if (user == null || user.DeletedAt.HasValue)
            throw new DomainException("magic_link", "Account not found.");

        // Ensure the email is marked verified
        if (!user.EmailVerified)
        {
            user.MarkEmailVerified();
            _userRepo.Update(user);
        }

        var scopes = new[] { "openid", "profile", "email" };
        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(null);

        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            Guid.Empty, user.Id, scopes, "magic_link", ipAddress, userAgent, expiresIn);
        var (refreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(
            Guid.Empty, user.Id, null, scopes, 30 * 24 * 60, // 30-day refresh
            ipAddress: ipAddress, userAgent: userAgent);

        user.RecordSuccessfulLogin();
        _userRepo.Update(user);

        await RecordLoginAttemptAsync(user.Id, user.Email, null,
            ipAddress, userAgent, true, null, null);
        await _unitOfWork.SaveChangesAsync();

        var userRoles = await _userRoleRepo.GetByUserIdAsync(user.Id);
        var roleNames = new List<string>();
        foreach (var ur in userRoles)
        {
            var role = await _roleRepo.GetByIdAsync(ur.RoleId);
            if (role != null) roleNames.Add(role.RoleCode);
        }

        return new LoginResponse
        {
            RequiresTwoFactor = false,
            AccessToken       = accessToken,
            RefreshToken      = refreshToken,
            ExpiresIn         = expiresIn,
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
}
