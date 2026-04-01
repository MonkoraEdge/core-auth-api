using System.ComponentModel.DataAnnotations;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; }

    [Required]
    [MaxLength(128)]
    public string Password { get; set; }

    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(10)]
    public string? Locale { get; set; }

    public Guid? TenantId { get; set; }

    [MaxLength(100)]
    public string? ClientId { get; set; }
}

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; }

    [MaxLength(100)]
    public string? ClientId { get; set; }
}

public class ResetPasswordRequest
{
    [Required]
    [MaxLength(512)]
    public string Token { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; }

    [Required]
    [MaxLength(128)]
    public string NewPassword { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    [MaxLength(128)]
    public string CurrentPassword { get; set; }

    [Required]
    [MaxLength(128)]
    public string NewPassword { get; set; }
}

public class VerifyEmailRequest
{
    [Required]
    [MaxLength(512)]
    public string Token { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; }
}

public class ResendVerificationEmailRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; }
}

public class LogoutRequest
{
    [MaxLength(512)]
    public string? RefreshToken { get; set; }

    public bool LogoutAllDevices { get; set; }
}

public class RefreshTokenRequest
{
    [Required]
    [MaxLength(512)]
    public string RefreshToken { get; set; }

    [MaxLength(100)]
    public string? ClientId { get; set; }

    [MaxLength(256)]
    public string? ClientSecret { get; set; }
}

public class TwoFactorSetupRequest
{
    [Required]
    [RegularExpression("^(TOTP|SMS|EMAIL)$", ErrorMessage = "DeviceType must be TOTP, SMS, or EMAIL.")]
    public string DeviceType { get; set; }  // "TOTP" | "SMS" | "EMAIL"

    [MaxLength(32)]
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
    [Required]
    [MaxLength(12)]
    public string Code { get; set; }

    [Required]
    [RegularExpression("^(TOTP|SMS|EMAIL)$", ErrorMessage = "DeviceType must be TOTP, SMS, or EMAIL.")]
    public string DeviceType { get; set; }
}

public class TwoFactorDisableRequest
{
    [Required]
    [MaxLength(12)]
    public string Code { get; set; }

    [Required]
    [MaxLength(128)]
    public string Password { get; set; }
}
