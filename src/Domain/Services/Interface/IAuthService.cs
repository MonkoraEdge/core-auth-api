using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
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

    /// <summary>Send a one-time magic-link login email to the given address.</summary>
    Task SendMagicLinkAsync(string email, string? clientId, string? redirectUri, string? ipAddress);

    /// <summary>Verify the magic-link token and issue session tokens.</summary>
    Task<LoginResponse> VerifyMagicLinkAsync(string token, string? ipAddress, string? userAgent);

    /// <summary>Setup 2FA for a user</summary>
    Task<TwoFactorSetupResponse> SetupTwoFactorAsync(Guid userId, TwoFactorSetupRequest request);

    /// <summary>Enable 2FA after verifying the code</summary>
    Task<TwoFactorEnableResponse> EnableTwoFactorAsync(Guid userId, TwoFactorVerifyRequest request);

    /// <summary>Disable 2FA</summary>
    Task<UpdateResponse> DisableTwoFactorAsync(Guid userId, TwoFactorDisableRequest request);

    /// <summary>Verify 2FA code during login using the opaque challenge token issued in the login response</summary>
    Task<LoginResponse> VerifyTwoFactorLoginAsync(string twoFactorToken, string code, string deviceType, string? ipAddress, string? userAgent);
}

/// <summary>
/// Social / external-identity-provider login flow.
/// Handles the OAuth2/OIDC authorization-code callback and local account linking.
/// </summary>
public interface ISocialLoginService
{
    /// <summary>
    /// Build the redirect URL that sends the user-agent to the external provider for authentication.
    /// </summary>
    Task<SocialLoginInitiateResponse> InitiateAsync(string providerCode, string? redirectUri, string? state, string? ipAddress);

    /// <summary>
    /// Handle the authorization-code callback from the external provider.
    /// Exchanges the code for tokens, upserts the local user, creates/links the external login record,
    /// and returns a local access + refresh token pair.
    /// </summary>
    Task<LoginResponse> HandleCallbackAsync(string providerCode, string code, string? state, string? ipAddress, string? userAgent);
}
