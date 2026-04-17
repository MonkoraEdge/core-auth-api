using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// Authentication endpoints for end-user account lifecycle:
/// sign-in, registration, password recovery, email verification, and 2FA operations.
/// </summary>
[Route("auth")]
[ApiController]
[ApiVersion("1.0")]
public class AuthController : MonkoraControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOAuth2Service _oauth2Service;
    private readonly ISocialLoginService _socialLoginService;

    public AuthController(IAuthService authService, IOAuth2Service oauth2Service, ISocialLoginService socialLoginService)
    {
        _authService = authService;
        _oauth2Service = oauth2Service;
        _socialLoginService = socialLoginService;
    }

    // ─── Login / Register / Token ─────────────────────────────────────────────

    /// <summary>
    /// Authenticate a user with username/password and optional second factor payload.
    /// Returns access/refresh tokens, or a 2FA challenge response when required.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request, GetIpAddress(), GetUserAgent());
        return Ok(result);
    }

    /// <summary>
    /// Register a new local account and trigger verification workflow.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("default")]
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
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _oauth2Service.ProcessTokenRequestAsync(
                new TokenRequest
                {
                    GrantType = "REFRESH_TOKEN",
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
        catch (DomainException ex)
        {
            var isInvalidGrant = ex.ErrorCode == ErrorCodeType.INVALID_GRANT
                || ex.ErrorCode == ErrorCodeType.TOKEN_REVOKED
                || ex.ErrorCode == ErrorCodeType.TOKEN_EXPIRED;
            var error = isInvalidGrant ? "invalid_grant" : "invalid_request";
            return BadRequest(new { error, error_description = ex.ErrorMessage });
        }
    }

    // ─── Password Management ──────────────────────────────────────────────────

    /// <summary>
    /// Start forgot-password flow and send reset instructions (email/link) if account exists.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Complete password reset using a valid reset token issued by forgot-password flow.
    /// </summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("default")]
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
    [EnableRateLimiting("default")]
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
    [EnableRateLimiting("default")]
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
    [EnableRateLimiting("default")]
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
    [EnableRateLimiting("default")]
    public async Task<IActionResult> TwoFactorVerify([FromBody] TwoFactorLoginRequest request)
    {
        var result = await _authService.VerifyTwoFactorLoginAsync(
            request.TwoFactorToken, request.Code, request.DeviceType, GetIpAddress(), GetUserAgent());
        return Ok(result);
    }

    // ─── Social Login ─────────────────────────────────────────────────────────

    /// <summary>
    /// Initiate a social login flow by redirecting the user to the external provider.
    /// Returns the authorization URL (with PKCE challenge) that the client should redirect to.
    /// </summary>
    [HttpGet("social/{providerCode}")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> SocialInitiate(string providerCode, [FromQuery] string? redirectUri, [FromQuery] string? state)
    {
        var result = await _socialLoginService.InitiateAsync(providerCode, redirectUri, state, GetIpAddress());
        return Ok(result);
    }

    /// <summary>
    /// Handle the authorization-code callback from the external identity provider.
    /// Exchanges the code for provider tokens, upserts the local user account,
    /// and returns local access + refresh tokens.
    /// </summary>
    [HttpGet("social/{providerCode}/callback")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> SocialCallback(string providerCode, [FromQuery] string code, [FromQuery] string? state)
    {
        var result = await _socialLoginService.HandleCallbackAsync(providerCode, code, state, GetIpAddress(), GetUserAgent());
        return Ok(result);
    }

}
