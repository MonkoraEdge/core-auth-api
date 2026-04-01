using System.ComponentModel.DataAnnotations;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;

public class LoginRequest
{
    [Required]
    [MaxLength(256)]
    public string Username { get; set; }

    [Required]
    [MaxLength(128)]
    public string Password { get; set; }

    [MaxLength(100)]
    public string? ClientId { get; set; }

    [MaxLength(12)]
    public string? TwoFactorCode { get; set; }

    [MaxLength(64)]
    public string? TwoFactorRecoveryCode { get; set; }

    [MaxLength(256)]
    public string? DeviceFingerprint { get; set; }

    [MaxLength(200)]
    public string? DeviceName { get; set; }
}

public class LoginResponse
{
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorToken { get; set; }     // temp token for 2FA step
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public int? ExpiresIn { get; set; }
    public UserInfoResult? User { get; set; }
}

public class UserInfoResult
{
    public string Id { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool EmailVerified { get; set; }
    public string Status { get; set; }
    public List<string> Roles { get; set; } = new();
}
