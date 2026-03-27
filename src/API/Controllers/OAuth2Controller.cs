using System.Text;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// OAuth2/OIDC protocol endpoints exposed by this authorization server.
/// Covers authorize, token issuance, revocation, introspection, userinfo, and logout session handling.
/// </summary>
[Route("oauth2")]
[ApiController]
public class OAuth2Controller : ControllerBase
{
    private readonly IOAuth2Service _oauth2Service;

    public OAuth2Controller(IOAuth2Service oauth2Service)
    {
        _oauth2Service = oauth2Service;
    }

    /// <summary>
    /// OAuth2 authorize endpoint.
    /// Validates authorize parameters and returns one of: redirect with code, login required, or consent required.
    /// </summary>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequest request)
    {
        var response = await _oauth2Service.ProcessAuthorizeRequestAsync(request, GetAuthenticatedUserId());
        return ToActionResult(response);
    }

    /// <summary>
    /// Consent submission endpoint used after UI consent screen.
    /// Performs same-origin guard for browser cookie contexts and returns redirect with success/error.
    /// </summary>
    [HttpPost("authorize/consent")]
    [Authorize]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> SubmitConsent([FromBody] ConsentSubmitRequest request)
    {
        if (!IsSameOriginBrowserPost())
            return Forbid();

        var userId = GetAuthenticatedUserId();
        if (userId == null) return Unauthorized();

        var response = await _oauth2Service.ProcessConsentAsync(new ConsentRequest
        {
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            Scope = request.Scope,
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Nonce = request.Nonce,
            Approved = request.Approved,
            RememberConsent = request.RememberConsent
        }, userId.Value);

        return Redirect(response.RedirectUrl);
    }

    /// <summary>
    /// Token endpoint.
    /// Supports authorization_code, refresh_token, and client_credentials grants.
    /// </summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Token([FromForm] TokenFormRequest formRequest)
    {
        var request = MapFormToTokenRequest(formRequest);
        ExtractClientCredentials(out var clientId, out var clientSecret, request);

        var response = await _oauth2Service.ProcessTokenRequestAsync(
            request, clientId, clientSecret, GetIpAddress(), GetUserAgent());
        return Ok(response);
    }

    /// <summary>
    /// Token revocation endpoint (RFC 7009).
    /// Revokes caller-owned token and always responds HTTP 200 per RFC behavior.
    /// </summary>
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Revoke([FromForm] RevocationFormRequest formRequest)
    {
        var request = new RevocationRequest
        {
            Token = formRequest.Token,
            TokenTypeHint = formRequest.TokenTypeHint
        };
        ExtractClientCredentials(out var clientId, out var clientSecret, null);
        await _oauth2Service.RevokeAsync(request, clientId ?? string.Empty, clientSecret);
        return Ok(); // RFC 7009: always 200
    }

    /// <summary>
    /// Token introspection endpoint (RFC 7662).
    /// Returns active/inactive token metadata for authorized clients.
    /// </summary>
    [HttpPost("introspect")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Introspect([FromForm] IntrospectFormRequest formRequest)
    {
        var request = new IntrospectRequest
        {
            Token = formRequest.Token,
            TokenTypeHint = formRequest.TokenTypeHint
        };
        ExtractClientCredentials(out var clientId, out var clientSecret, null);
        var result = await _oauth2Service.IntrospectAsync(request, clientId ?? string.Empty, clientSecret);
        return Ok(result);
    }

    /// <summary>
    /// OIDC UserInfo endpoint that returns allowed claims for authenticated subject.
    /// </summary>
    [HttpGet("userinfo")]
    [Authorize]
    public async Task<IActionResult> UserInfo()
    {
        var headerValue = Request.Headers[HeaderNames.Authorization].ToString();
        var token = headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? headerValue["Bearer ".Length..].Trim()
            : string.Empty;
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var result = await _oauth2Service.GetUserInfoAsync(token);
        return Ok(result);
    }

    /// <summary>
    /// End session endpoint for RP-initiated logout.
    /// Revokes active sessions and optionally redirects to post-logout URI.
    /// </summary>
    [HttpGet("end-session")]
    [HttpPost("end-session")]
    public async Task<IActionResult> EndSession([FromQuery] string? id_token_hint, [FromQuery] string? post_logout_redirect_uri)
    {
        var userId = GetAuthenticatedUserId();
        if (userId.HasValue)
            await _oauth2Service.EndSessionAsync(userId.Value, id_token_hint);

        if (!string.IsNullOrEmpty(post_logout_redirect_uri)
            && Uri.TryCreate(post_logout_redirect_uri, UriKind.Absolute, out var redirectUri)
            && (redirectUri.Scheme == Uri.UriSchemeHttps || redirectUri.Scheme == Uri.UriSchemeHttp))
            return Redirect(post_logout_redirect_uri);

        return Ok(new { message = "Session ended." });
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve authenticated subject id from JWT claims when available.
    /// </summary>
    private Guid? GetAuthenticatedUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>
    /// Extract OAuth client credentials from request body and Authorization basic header.
    /// </summary>
    private void ExtractClientCredentials(out string? clientId, out string? clientSecret, TokenRequest? request)
    {
        clientId = request?.ClientId;
        clientSecret = request?.ClientSecret;

        // Try Authorization: Basic <base64> header (client_secret_basic)
        var authHeader = Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader[6..]));
                var colonIndex = decoded.IndexOf(':');
                if (colonIndex > 0)
                {
                    clientId = decoded[..colonIndex];
                    clientSecret = decoded[(colonIndex + 1)..];
                }
            }
            catch { /* ignore malformed header */ }
        }
    }

    /// <summary>
    /// Resolve caller IP address using reverse-proxy header fallback.
    /// </summary>
    private string GetIpAddress() =>
        Request.Headers["X-Forwarded-For"].FirstOrDefault()
        ?? HttpContext.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    /// <summary>
    /// Same-origin guard for browser cookie requests to reduce CSRF risk on consent POST.
    /// </summary>
    private bool IsSameOriginBrowserPost()
    {
        // CSRF guard for browser form/cookie contexts. Non-browser clients typically don't send Cookie.
        if (!Request.Headers.ContainsKey("Cookie"))
            return true;

        var currentOrigin = $"{Request.Scheme}://{Request.Host.Value}";

        if (Request.Headers.TryGetValue("Origin", out var origin) && !string.IsNullOrWhiteSpace(origin))
            return string.Equals(origin.ToString(), currentOrigin, StringComparison.OrdinalIgnoreCase);

        if (Request.Headers.TryGetValue("Referer", out var referer)
            && Uri.TryCreate(referer.ToString(), UriKind.Absolute, out var refererUri))
        {
            var refererOrigin = $"{refererUri.Scheme}://{refererUri.Authority}";
            return string.Equals(refererOrigin, currentOrigin, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Resolve caller user-agent for security telemetry and token issuance context.
    /// </summary>
    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

    /// <summary>
    /// Convert domain-level authorize result into API response shape.
    /// </summary>
    private IActionResult ToActionResult(AuthorizeEndpointResponse response)
    {
        return response.Kind switch
        {
            AuthorizeResponseKind.Redirect => Redirect(response.RedirectUrl!),
            AuthorizeResponseKind.LoginRequired => Ok(new
            {
                requires_login = true,
                client = response.Client,
                requested_scopes = response.RequestedScopes
            }),
            AuthorizeResponseKind.ConsentRequired => Ok(new
            {
                requires_consent = true,
                client = response.Client,
                requested_scopes = response.RequestedScopes
            }),
            _ => BadRequest(new { error = response.Error, error_description = response.ErrorDescription })
        };
    }

    /// <summary>
    /// Map form-urlencoded token request payload to domain token request model.
    /// </summary>
    private static TokenRequest MapFormToTokenRequest(TokenFormRequest f) => new()
    {
        GrantType = f.GrantType,
        Code = f.Code,
        RedirectUri = f.RedirectUri,
        CodeVerifier = f.CodeVerifier,
        RefreshToken = f.RefreshToken,
        Scope = f.Scope,
        ClientId = f.ClientId,
        ClientSecret = f.ClientSecret
    };
}
