using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface IAuthService
{
    /// <summary>Authenticate user with username/password, returns tokens or 2FA challenge</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);

    /// <summary>Register a new user account</summary>
    Task<CreateResponse> RegisterAsync(RegisterRequest request, string? ipAddress);

    /// <summary>Logout a session, revoke tokens</summary>
    Task LogoutAsync(Guid userId, string? refreshToken, bool allDevices, string? ipAddress);

    /// <summary>Refresh access token using a valid refresh token</summary>
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? clientId, string? clientSecret, string? ipAddress, string? userAgent);

    /// <summary>Send password reset email</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>Reset password using token</summary>
    Task<UpdateResponse> ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>Change password (authenticated user)</summary>
    Task<UpdateResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    /// <summary>Send email verification</summary>
    Task SendVerificationEmailAsync(Guid userId, string email);

    /// <summary>Verify email with token</summary>
    Task<UpdateResponse> VerifyEmailAsync(VerifyEmailRequest request);

    /// <summary>Setup 2FA for a user</summary>
    Task<TwoFactorSetupResponse> SetupTwoFactorAsync(Guid userId, TwoFactorSetupRequest request);

    /// <summary>Enable 2FA after verifying the code</summary>
    Task<UpdateResponse> EnableTwoFactorAsync(Guid userId, TwoFactorVerifyRequest request);

    /// <summary>Disable 2FA</summary>
    Task<UpdateResponse> DisableTwoFactorAsync(Guid userId, TwoFactorDisableRequest request);

    /// <summary>Verify 2FA code during login</summary>
    Task<LoginResponse> VerifyTwoFactorLoginAsync(Guid userId, string code, string deviceType, string? ipAddress, string? userAgent);
}
