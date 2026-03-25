namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string? ClientId { get; set; }
    public string? TwoFactorCode { get; set; }
    public string? TwoFactorRecoveryCode { get; set; }
    public string? DeviceFingerprint { get; set; }
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
