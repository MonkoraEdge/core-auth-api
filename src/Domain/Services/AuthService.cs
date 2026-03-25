using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuditAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

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
    private readonly int _signinFailedMinutes;

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
                throw new CustomHttpBadRequestException("login", "Too many failed attempts from this IP. Try again later.");
        }

        var user = await _userRepo.GetByEmailAsync(request.Username);
        if (user == null)
        {
            await RecordLoginAttemptAsync(null, request.Username, null, ipAddress, userAgent, false, "user_not_found", request.ClientId);
            throw new CustomHttpBadRequestException("login", "Invalid username or password.");
        }

        var identity = await _identityRepo.GetByUserIdAsync(user.Id);
        if (identity == null || string.IsNullOrEmpty(identity.PasswordHash))
        {
            await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "no_password", request.ClientId);
            throw new CustomHttpBadRequestException("login", "Invalid username or password.");
        }

        if (identity.LockedUntil.HasValue && identity.LockedUntil > DateTime.UtcNow)
        {
            await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "account_locked", request.ClientId);
            throw new CustomHttpBadRequestException("login", $"Account is locked until {identity.LockedUntil:u}.");
        }

        if (!user.IsActive || user.Status == "SUSPENDED")
        {
            await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "account_inactive", request.ClientId);
            throw new CustomHttpBadRequestException("login", "Account is disabled.");
        }

        if (!_passwordService.VerifyPassword(request.Password, identity.PasswordHash))
        {
            identity.FailedAttempts++;
            if (identity.FailedAttempts >= 5)
                identity.LockedUntil = DateTime.UtcNow.AddMinutes(_signinFailedMinutes);
            _identityRepo.Update(identity);

            await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "invalid_password", request.ClientId);
            await _unitOfWork.SaveChangesAsync();
            throw new CustomHttpBadRequestException("login", "Invalid username or password.");
        }

        identity.FailedAttempts = 0;
        identity.LockedUntil = null;
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

                return new LoginResponse
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = _passwordService.GenerateSecureToken(32)
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
                var codeHash = _passwordService.HashSha256(request.TwoFactorRecoveryCode.ToUpperInvariant());
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
                await RecordLoginAttemptAsync(user.Id, request.Username, null, ipAddress, userAgent, false, "invalid_2fa_code", request.ClientId);
                await _unitOfWork.SaveChangesAsync();
                throw new CustomHttpBadRequestException("login", "Invalid two-factor authentication code.");
            }
        }

        return await IssueTokensAndFinalizeLoginAsync(user, request.ClientId, ipAddress, userAgent);
    }

    public async Task<CreateResponse> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        if (!_passwordService.MeetsPasswordPolicy(request.Password))
            throw new CustomHttpBadRequestException("register", "Password does not meet policy requirements (min 8 chars, upper, lower, digit, special).");

        var existingUser = await _userRepo.GetByEmailAsync(request.Email.ToLowerInvariant());
        if (existingUser != null)
            throw new CustomHttpBadRequestException("register", "Email address is already registered.");

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
                await _tokenService.RevokeTokenAsync(refreshToken, "refresh_token", rt.ClientId, "logout");
        }

        var user = await _userRepo.GetByIdAsync(userId);
        if (user != null)
        {
            user.LastActivityAt = DateTime.UtcNow;
            _userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? clientId, string? ipAddress, string? userAgent)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        var rt = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);

        if (rt == null)
            throw new CustomHttpBadRequestException("refresh_token", "Invalid or expired refresh token.");

        if (rt.RevokedAt.HasValue)
        {
            if (rt.FamilyId != Guid.Empty)
                await _tokenService.RevokeTokenFamilyAsync(rt.FamilyId, "refresh_token_reuse_detected");

            throw new CustomHttpBadRequestException("refresh_token",
                "The refresh token has already been used. All sessions in this chain have been revoked for security.");
        }

        if (rt.ExpiresAt <= DateTime.UtcNow)
            throw new CustomHttpBadRequestException("refresh_token", "Invalid or expired refresh token.");

        if (!string.IsNullOrEmpty(clientId))
        {
            var client = await _clientRepo.GetByClientIdAsync(clientId);
            if (client == null || client.Id != rt.ClientId)
                throw new CustomHttpBadRequestException("refresh_token", "Client mismatch.");
        }

        // Rotate: revoke old, issue new
        rt.RevokedAt = DateTime.UtcNow;
        _refreshTokenRepo.Update(rt);

        var scopes = rt.Scopes;
        var newAccessToken = await _tokenService.GenerateAccessTokenAsync(
            rt.ClientId, rt.UserId == Guid.Empty ? null : rt.UserId, scopes, "refresh_token", ipAddress, userAgent);
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(
            Guid.Empty, rt.ClientId, rt.UserId == Guid.Empty ? null : rt.UserId, rt.SessionId, scopes,
            (int)(rt.ExpiresAt - rt.IssuedAt).TotalSeconds, familyId: rt.FamilyId);

        await _unitOfWork.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            RefreshToken = newRefreshToken,
            Scope = string.Join(" ", scopes)
        };
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
        // TODO: dispatch email notification with rawToken
    }

    public async Task<UpdateResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (!_passwordService.MeetsPasswordPolicy(request.NewPassword))
            throw new CustomHttpBadRequestException("reset_password", "Password does not meet policy requirements.");

        var tokenHash = _passwordService.HashSha256(request.Token);
        var reset = await _passwordResetRepo.GetByTokenHashAsync(tokenHash);

        if (reset == null || reset.ExpiresAt <= DateTime.UtcNow || reset.UsedAt.HasValue)
            throw new CustomHttpBadRequestException("reset_password", "Reset token is invalid or has expired.");

        var user = await _userRepo.GetByIdAsync(reset.UserId);
        if (user == null || !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            throw new CustomHttpBadRequestException("reset_password", "Invalid request.");

        var identity = await _identityRepo.GetByUserIdAsync(user.Id);
        if (identity == null)
            throw new CustomHttpBadRequestException("reset_password", "User identity not found.");

        var history = await _passwordHistoryRepo.GetByUserIdAsync(user.Id, 5);
        if (history.Any(h => _passwordService.VerifyPassword(request.NewPassword, h.PasswordHash)))
            throw new CustomHttpBadRequestException("reset_password", "Cannot reuse a recent password.");

        var oldHash = identity.PasswordHash ?? string.Empty;
        identity.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        identity.PasswordAlgo = "BCRYPT";
        identity.PasswordUpdatedAt = DateTime.UtcNow;
        identity.FailedAttempts = 0;
        identity.LockedUntil = null;
        _identityRepo.Update(identity);

        reset.UsedAt = DateTime.UtcNow;
        _passwordResetRepo.Update(reset);

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
            throw new CustomHttpBadRequestException("change_password", "Password does not meet policy requirements.");

        var identity = await _identityRepo.GetByUserIdAsync(userId);
        if (identity == null || string.IsNullOrEmpty(identity.PasswordHash))
            throw new CustomHttpBadRequestException("change_password", "Identity not found.");

        if (!_passwordService.VerifyPassword(request.CurrentPassword, identity.PasswordHash))
            throw new CustomHttpBadRequestException("change_password", "Current password is incorrect.");

        var history = await _passwordHistoryRepo.GetByUserIdAsync(userId, 5);
        if (history.Any(h => _passwordService.VerifyPassword(request.NewPassword, h.PasswordHash)))
            throw new CustomHttpBadRequestException("change_password", "Cannot reuse a recent password.");

        var oldHash = identity.PasswordHash;
        identity.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        identity.PasswordAlgo = "BCRYPT";
        identity.PasswordUpdatedAt = DateTime.UtcNow;
        _identityRepo.Update(identity);

        _passwordHistoryRepo.Insert(new PasswordHistory { UserId = userId, PasswordHash = oldHash });

        var user = await _userRepo.GetByIdAsync(userId);
        if (user != null) { user.LastPasswordChangedAt = DateTime.UtcNow; _userRepo.Update(user); }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = userId, IsSuccess = true, Message = "Password changed." };
    }

    public async Task SendVerificationEmailAsync(Guid userId, string email)
    {
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
        // TODO: dispatch email notification with rawToken
    }

    public async Task<UpdateResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var tokenHash = _passwordService.HashSha256(request.Token);
        var verif = await _emailVerifRepo.GetByTokenHashAsync(tokenHash);

        if (verif == null || verif.ExpiresAt <= DateTime.UtcNow || verif.VerifiedAt.HasValue)
            throw new CustomHttpBadRequestException("verify_email", "Token is invalid or has expired.");

        if (!string.Equals(verif.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            throw new CustomHttpBadRequestException("verify_email", "Email does not match.");

        verif.VerifiedAt = DateTime.UtcNow;
        _emailVerifRepo.Update(verif);

        var user = await _userRepo.GetByIdAsync(verif.UserId);
        if (user != null)
        {
            user.EmailVerified = true;
            if (user.Status == "INACTIVE") user.Status = "ACTIVE";
            _userRepo.Update(user);
        }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = verif.UserId, IsSuccess = true, Message = "Email verified." };
    }

    public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(Guid userId, TwoFactorSetupRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) throw new CustomHttpBadRequestException("2fa_setup", "User not found.");

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

        response.RecoveryCodes = _passwordService.GenerateRecoveryCodes(10);
        return response;
    }

    public async Task<UpdateResponse> EnableTwoFactorAsync(Guid userId, TwoFactorVerifyRequest request)
    {
        var settings = await _twoFactorRepo.GetByUserIdAsync(userId);
        var pending = settings.FirstOrDefault(s => s.DeviceType == request.DeviceType && !s.IsActive);
        if (pending == null)
            throw new CustomHttpBadRequestException("2fa_enable", "No pending 2FA setup found for this device type.");

        bool valid = request.DeviceType == "TOTP"
            && _passwordService.VerifyTotpCode(pending.SecretKey!, request.Code);

        if (!valid)
            throw new CustomHttpBadRequestException("2fa_enable", "Invalid verification code.");

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
        return new UpdateResponse { Id = userId, IsSuccess = true, Message = "Two-factor authentication enabled." };
    }

    public async Task<UpdateResponse> DisableTwoFactorAsync(Guid userId, TwoFactorDisableRequest request)
    {
        var identity = await _identityRepo.GetByUserIdAsync(userId);
        if (identity == null || !_passwordService.VerifyPassword(request.Password, identity.PasswordHash ?? ""))
            throw new CustomHttpBadRequestException("2fa_disable", "Invalid password.");

        var settings = await _twoFactorRepo.GetByUserIdAsync(userId);
        foreach (var s in settings.Where(s => s.IsActive))
        {
            s.IsActive = false;
            _twoFactorRepo.Update(s);
        }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = userId, IsSuccess = true, Message = "Two-factor authentication disabled." };
    }

    public async Task<LoginResponse> VerifyTwoFactorLoginAsync(Guid userId, string code, string deviceType, string? ipAddress, string? userAgent)
    {
        var settings = await _twoFactorRepo.GetByUserIdAsync(userId);
        var setting = settings.FirstOrDefault(s => s.DeviceType == deviceType && s.IsActive);
        if (setting == null)
            throw new CustomHttpBadRequestException("2fa_verify", "2FA is not configured for this device type.");

        bool valid = deviceType == "TOTP"
            && _passwordService.VerifyTotpCode(setting.SecretKey!, code);

        if (!valid)
            throw new CustomHttpBadRequestException("2fa_verify", "Invalid 2FA code.");

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) throw new CustomHttpBadRequestException("2fa_verify", "User not found.");

        return await IssueTokensAndFinalizeLoginAsync(user, null, ipAddress, userAgent);
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

        var accessToken = await _tokenService.GenerateAccessTokenAsync(
            resolvedClientId, user.Id, scopes, "password", ipAddress, userAgent);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(
            Guid.Empty, resolvedClientId, user.Id, null, scopes, refreshLifetime);

        user.LastLoginAt = DateTime.UtcNow;
        user.LastActivityAt = DateTime.UtcNow;
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
            ExpiresIn = accessLifetime,
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
            LoginMethod = "PASSWORD"
        });
    }
}
