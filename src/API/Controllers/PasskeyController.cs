using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// WebAuthn / FIDO2 passkey endpoints.
/// Registration requires an authenticated session; authentication endpoints are public.
/// </summary>
[Route("auth/passkey")]
[ApiController]
[ApiVersion("1.0")]
public class PasskeyController : MonkoraControllerBase
{
    private readonly IPasskeyService _passkeyService;
    private readonly IUserRepository _userRepo;

    public PasskeyController(IPasskeyService passkeyService, IUserRepository userRepo)
    {
        _passkeyService = passkeyService;
        _userRepo       = userRepo;
    }

    // ─── Registration ─────────────────────────────────────────────────────────

    /// <summary>
    /// Begin the passkey registration ceremony for the authenticated user.
    /// Returns JSON <c>CredentialCreateOptions</c> to pass to the browser's
    /// <c>navigator.credentials.create()</c> call.
    /// </summary>
    [HttpPost("register/begin")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterBegin()
    {
        var userId  = GetUserId();
        var user    = await _userRepo.GetByIdAsync(userId);
        var email   = user?.Email ?? string.Empty;
        var displayName = user?.DisplayName;

        var envelope = await _passkeyService.BeginRegistrationAsync(userId, email, displayName);

        // envelope is { challengeId, options }
        return Ok(envelope);
    }

    /// <summary>
    /// Complete the passkey registration ceremony.
    /// The request body must contain the challengeId from the begin step and the
    /// JSON-serialised attestation response from the browser.
    /// </summary>
    [HttpPost("register/complete")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterComplete([FromBody] PasskeyRegisterCompleteRequest request)
    {
        var userId = GetUserId();

        // Wrap attestationResponse + challengeId into the envelope PasskeyService expects.
        var envelope = System.Text.Json.JsonSerializer.Serialize(new
        {
            challengeId         = request.ChallengeId,
            attestationResponse = request.AttestationResponse
        });

        var credential = await _passkeyService.CompleteRegistrationAsync(
            userId, envelope, request.FriendlyName);

        return Ok(MapToDto(credential));
    }

    // ─── Authentication ────────────────────────────────────────────────────────

    /// <summary>
    /// Begin the passkey authentication ceremony.
    /// Returns JSON <c>AssertionOptions</c> and a <c>challengeId</c> that must be
    /// echoed back in the complete step.
    /// </summary>
    [HttpPost("authenticate/begin")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> AuthBegin([FromBody] PasskeyAuthBeginRequest? request)
    {
        var (optionsJson, challengeId) =
            await _passkeyService.BeginAuthenticationAsync(request?.Email);

        return Ok(new PasskeyOptionsResponse
        {
            ChallengeId = challengeId,
            Options     = optionsJson
        });
    }

    /// <summary>
    /// Complete the passkey authentication ceremony.
    /// On success returns JWT access + refresh tokens, identical to the password login response.
    /// </summary>
    [HttpPost("authenticate/complete")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> AuthComplete([FromBody] PasskeyAuthCompleteRequest request)
    {
        var result = await _passkeyService.CompleteAuthenticationAsync(
            request.ChallengeId,
            request.AssertionResponse,
            GetIpAddress(),
            GetUserAgent());

        return Ok(result);
    }

    // ─── Credential management ─────────────────────────────────────────────────

    /// <summary>List all passkeys registered by the authenticated user.</summary>
    [HttpGet("credentials")]
    [Authorize]
    public async Task<IActionResult> ListCredentials()
    {
        var credentials = await _passkeyService.GetCredentialsAsync(GetUserId());
        return Ok(credentials.Select(MapToDto));
    }

    /// <summary>Remove a specific passkey credential owned by the authenticated user.</summary>
    [HttpDelete("credentials/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteCredential([FromRoute] Guid id)
    {
        await _passkeyService.DeleteCredentialAsync(GetUserId(), id);
        return NoContent();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static PasskeyCredentialDto MapToDto(PasskeyCredential c) => new()
    {
        Id                    = c.Id,
        CredentialIdBase64Url = c.CredentialIdBase64Url,
        FriendlyName          = c.FriendlyName,
        AaGuid                = c.AaGuid,
        Transports            = c.Transports,
        IsBackupEligible      = c.IsBackupEligible,
        IsBackedUp            = c.IsBackedUp,
        AttestationType       = c.AttestationType,
        LastUsedAt            = c.LastUsedAt,
        IsActive              = c.IsActive,
        CreatedAt             = c.CreatedAt
    };
}
