using System.Security.Claims;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// Authentication endpoints for end-user account lifecycle:
/// sign-in, registration, password recovery, email verification, and 2FA operations.
/// </summary>
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

    /// <summary>
    /// Authenticate a user with username/password and optional second factor payload.
    /// Returns access/refresh tokens, or a 2FA challenge response when required.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request, GetIpAddress(), GetUserAgent());
        return Ok(result);
    }

    /// <summary>
    /// Register a new local account and trigger verification workflow.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request, GetIpAddress());
        return Ok(result);
    }

    /// <summary>
    /// Sign out the current user and revoke refresh tokens for one device or all devices.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        await _authService.LogoutAsync(
            GetUserId(), request.RefreshToken, request.LogoutAllDevices, GetIpAddress());
        return NoContent();
    }

    /// <summary>
    /// Compatibility refresh endpoint that proxies to OAuth2 refresh_token grant processing.
    /// </summary>
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

    /// <summary>
    /// Start forgot-password flow and send reset instructions (email/link) if account exists.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Complete password reset using a valid reset token issued by forgot-password flow.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Change password for currently authenticated user after validating current credentials.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(GetUserId(), request);
        return Ok(result);
    }

    // ─── Email Verification ───────────────────────────────────────────────────

    /// <summary>
    /// Verify ownership of email address via verification token.
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var result = await _authService.VerifyEmailAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Re-send email verification message for current user context.
    /// </summary>
    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailRequest request)
    {
        await _authService.SendVerificationEmailAsync(GetUserId(), request.Email);
        return NoContent();
    }

    // ─── Two-Factor Authentication ────────────────────────────────────────────

    /// <summary>
    /// Generate or fetch 2FA setup materials (for example TOTP secret and QR payload).
    /// </summary>
    [HttpGet("2fa/setup")]
    [Authorize]
    public async Task<IActionResult> TwoFactorSetup([FromQuery] string deviceType = "TOTP")
    {
        var result = await _authService.SetupTwoFactorAsync(
            GetUserId(), new TwoFactorSetupRequest { DeviceType = deviceType });
        return Ok(result);
    }

    /// <summary>
    /// Enable 2FA for current user after successful verification of one-time code.
    /// </summary>
    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> TwoFactorEnable([FromBody] TwoFactorVerifyRequest request)
    {
        var result = await _authService.EnableTwoFactorAsync(GetUserId(), request);
        return Ok(result);
    }

    /// <summary>
    /// Disable 2FA for current user. Service validates required proof such as password/code.
    /// </summary>
    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> TwoFactorDisable([FromBody] TwoFactorDisableRequest request)
    {
        var result = await _authService.DisableTwoFactorAsync(GetUserId(), request);
        return Ok(result);
    }

    /// <summary>
    /// Finalize login when account is protected by 2FA challenge.
    /// </summary>
    [HttpPost("2fa/verify")]
    public async Task<IActionResult> TwoFactorVerify([FromBody] TwoFactorLoginRequest request)
    {
        var result = await _authService.VerifyTwoFactorLoginAsync(
            request.UserId, request.Code, request.DeviceType, GetIpAddress(), GetUserAgent());
        return Ok(result);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve authenticated user id from JWT claims (sub/nameidentifier).
    /// </summary>
    private Guid GetUserId()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var id)) throw new UnauthorizedAccessException("Invalid user token.");
        return id;
    }

    /// <summary>
    /// Resolve caller IP address using reverse-proxy header fallback.
    /// </summary>
    private string GetIpAddress() =>
        Request.Headers["X-Forwarded-For"].FirstOrDefault()
        ?? HttpContext.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    /// <summary>
    /// Resolve caller user-agent for security telemetry and anomaly checks.
    /// </summary>
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
