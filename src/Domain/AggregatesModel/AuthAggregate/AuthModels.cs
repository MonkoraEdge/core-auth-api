namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;

public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Locale { get; set; }
    public Guid? TenantId { get; set; }
    public string? ClientId { get; set; }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; }
    public string? ClientId { get; set; }
}

public class ResetPasswordRequest
{
    public string Token { get; set; }
    public string Email { get; set; }
    public string NewPassword { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}

public class VerifyEmailRequest
{
    public string Token { get; set; }
    public string Email { get; set; }
}

public class ResendVerificationEmailRequest
{
    public string Email { get; set; }
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
    public bool LogoutAllDevices { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; }
    public string? ClientId { get; set; }
}

public class TwoFactorSetupRequest
{
    public string DeviceType { get; set; }  // "TOTP" | "SMS" | "EMAIL"
    public string? PhoneNumber { get; set; }
}

public class TwoFactorSetupResponse
{
    public string DeviceType { get; set; }
    public string? TotpUri { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? PhoneNumber { get; set; }
    public string[]? RecoveryCodes { get; set; }
}

public class TwoFactorVerifyRequest
{
    public string Code { get; set; }
    public string DeviceType { get; set; }
}

public class TwoFactorDisableRequest
{
    public string Code { get; set; }
    public string Password { get; set; }
}
