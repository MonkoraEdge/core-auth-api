namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface IPasswordService
{
    /// <summary>Hash a plain password using BCrypt</summary>
    string HashPassword(string password);

    /// <summary>Verify a password against a stored BCrypt hash</summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>Check if password meets policy requirements</summary>
    bool MeetsPasswordPolicy(string password, int minLength = 8, bool requireUpper = true, bool requireNumber = true, bool requireSpecial = true);

    /// <summary>
    /// Run a dummy bcrypt verification that always returns false but takes the same time as a
    /// real bcrypt check. Call this when a user is not found to prevent username enumeration
    /// via response-time side-channel.
    /// </summary>
    void PerformDummyVerify(string password);

    /// <summary>Generate a cryptographically secure random token</summary>
    string GenerateSecureToken(int length = 64);

    /// <summary>Generate a numeric OTP code</summary>
    string GenerateOtpCode(int digits = 6);

    /// <summary>Hash a value with SHA-256 (for API keys, reset tokens, etc.)</summary>
    string HashSha256(string value);

    /// <summary>Verify a PKCE code verifier against a code challenge</summary>
    bool VerifyPkceCodeVerifier(string codeVerifier, string codeChallenge, string codeChallengeMethod);

    /// <summary>Generate a TOTP secret key for 2FA</summary>
    string GenerateTotpSecret();

    /// <summary>Verify a TOTP code</summary>
    bool VerifyTotpCode(string secret, string code);

    /// <summary>Get TOTP provisioning URI (for QR code)</summary>
    string GetTotpUri(string secret, string email, string issuer);

    /// <summary>Generate recovery codes for 2FA</summary>
    string[] GenerateRecoveryCodes(int count = 10);
}
