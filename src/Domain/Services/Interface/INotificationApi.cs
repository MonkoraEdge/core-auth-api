namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Integration contract for the external Notification API.
/// The Identity Server owns OTP generation and validation logic;
/// delivery (email transport, SMS gateway) is fully delegated to this service.
/// </summary>
public interface INotificationApi
{
    /// <summary>
    /// Sends an OTP code via email.
    /// The auth server generates the code; this method only delivers it.
    /// </summary>
    Task SendEmailOtpAsync(
        string toEmail,
        string otp,
        string purpose,
        string? displayName = null,
        string? language = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an OTP code via SMS.
    /// </summary>
    Task SendSmsOtpAsync(
        string toPhone,
        string otp,
        string purpose,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an email verification link.
    /// </summary>
    Task SendEmailVerificationAsync(
        string toEmail,
        string verificationToken,
        string? displayName = null,
        string? language = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a password reset link.
    /// </summary>
    Task SendPasswordResetAsync(
        string toEmail,
        string resetToken,
        string? displayName = null,
        string? language = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a magic-link login email. The recipient clicks the link to sign in without a password.
    /// </summary>
    Task SendMagicLinkAsync(
        string toEmail,
        string magicToken,
        string? clientId = null,
        string? redirectUri = null,
        string? displayName = null,
        string? language = null,
        CancellationToken ct = default);
}
