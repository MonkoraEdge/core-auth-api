using System.Security.Claims;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>Authentication endpoints — login, register, password management, 2FA</summary>
[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOAuth2Service _oauth2Service;

    public AuthController(IAuthService authService, IOAuth2Service oauth2Service)
    {
        _authService = authService;
        _oauth2Service = oauth2Service;
    }

    // ─── Login / Register / Token ─────────────────────────────────────────────

    /// <summary>Login with username and password</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request, GetIpAddress(), GetUserAgent());
        return Ok(result);
    }

    /// <summary>Register a new user account</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request, GetIpAddress());
        return Ok(result);
    }

    /// <summary>Logout and revoke tokens</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        await _authService.LogoutAsync(
            GetUserId(), request.RefreshToken, request.LogoutAllDevices, GetIpAddress());
        return NoContent();
    }

    /// <summary>Refresh access token using refresh token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _oauth2Service.ProcessTokenRequestAsync(
            new TokenRequest
            {
                GrantType = "refresh_token",
                RefreshToken = request.RefreshToken,
                ClientId = request.ClientId,
                ClientSecret = request.ClientSecret
            },
            request.ClientId,
            request.ClientSecret,
            GetIpAddress(),
            GetUserAgent());
        return Ok(result);
    }

    // ─── Password Management ──────────────────────────────────────────────────

    /// <summary>Initiate forgot password flow — sends reset email/link</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return NoContent();
    }

    /// <summary>Reset password using the token received by email</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return NoContent();
    }

    /// <summary>Change password while authenticated</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(GetUserId(), request);
        return Ok(result);
    }

    // ─── Email Verification ───────────────────────────────────────────────────

    /// <summary>Verify email address using the token sent to the user</summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var result = await _authService.VerifyEmailAsync(request);
        return Ok(result);
    }

    /// <summary>Resend email verification message (requires authentication)</summary>
    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailRequest request)
    {
        await _authService.SendVerificationEmailAsync(GetUserId(), request.Email);
        return NoContent();
    }

    // ─── Two-Factor Authentication ────────────────────────────────────────────

    /// <summary>Get TOTP setup details (secret + QR URI)</summary>
    [HttpGet("2fa/setup")]
    [Authorize]
    public async Task<IActionResult> TwoFactorSetup([FromQuery] string deviceType = "TOTP")
    {
        var result = await _authService.SetupTwoFactorAsync(
            GetUserId(), new TwoFactorSetupRequest { DeviceType = deviceType });
        return Ok(result);
    }

    /// <summary>Enable 2FA after verifying TOTP code</summary>
    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> TwoFactorEnable([FromBody] TwoFactorVerifyRequest request)
    {
        var result = await _authService.EnableTwoFactorAsync(GetUserId(), request);
        return Ok(result);
    }

    /// <summary>Disable 2FA — requires password confirmation</summary>
    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> TwoFactorDisable([FromBody] TwoFactorDisableRequest request)
    {
        var result = await _authService.DisableTwoFactorAsync(GetUserId(), request);
        return Ok(result);
    }

    /// <summary>Complete login for 2FA-protected accounts (called after initial login challenge)</summary>
    [HttpPost("2fa/verify")]
    public async Task<IActionResult> TwoFactorVerify([FromBody] TwoFactorLoginRequest request)
    {
        var result = await _authService.VerifyTwoFactorLoginAsync(
            request.UserId, request.Code, request.DeviceType, GetIpAddress(), GetUserAgent());
        return Ok(result);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var id)) throw new UnauthorizedAccessException("Invalid user token.");
        return id;
    }

    private string GetIpAddress() =>
        Request.Headers["X-Forwarded-For"].FirstOrDefault()
        ?? HttpContext.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();
}

/// <summary>Request model for completing 2FA login challenge</summary>
public class TwoFactorLoginRequest
{
    /// <summary>User ID returned in the initial login response when 2FA is required</summary>
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "TOTP";
}
