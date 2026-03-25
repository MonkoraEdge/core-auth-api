using System.Text;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>OAuth2 authorization server endpoints (RFC 6749, RFC 7009, RFC 7662)</summary>
[Route("oauth2")]
[ApiController]
public class OAuth2Controller : ControllerBase
{
    private readonly IOAuth2Service _oauth2Service;

    public OAuth2Controller(IOAuth2Service oauth2Service)
    {
        _oauth2Service = oauth2Service;
    }

    /// <summary>Authorization endpoint — validates request and (if logged in) issues authorization code</summary>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequest request)
    {
        var response = await _oauth2Service.ProcessAuthorizeRequestAsync(request, GetAuthenticatedUserId());
        return ToActionResult(response);
    }

    /// <summary>Submit consent and receive authorization code</summary>
    [HttpPost("authorize/consent")]
    [Authorize]
    public async Task<IActionResult> SubmitConsent([FromBody] ConsentSubmitRequest request)
    {
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

    /// <summary>Token endpoint — exchange authorization code, client credentials, or refresh token for tokens</summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Token([FromForm] TokenFormRequest formRequest)
    {
        var request = MapFormToTokenRequest(formRequest);
        ExtractClientCredentials(out var clientId, out var clientSecret, request);

        var response = await _oauth2Service.ProcessTokenRequestAsync(
            request, clientId, clientSecret, GetIpAddress(), GetUserAgent());
        return Ok(response);
    }

    /// <summary>Token revocation endpoint (RFC 7009)</summary>
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
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

    /// <summary>Token introspection endpoint (RFC 7662)</summary>
    [HttpPost("introspect")]
    [Consumes("application/x-www-form-urlencoded")]
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

    /// <summary>UserInfo endpoint — returns claims for authenticated user</summary>
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

    /// <summary>End session endpoint (OIDC RP-Initiated Logout)</summary>
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

    private Guid? GetAuthenticatedUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

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

    private string GetIpAddress() =>
        Request.Headers["X-Forwarded-For"].FirstOrDefault()
        ?? HttpContext.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

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
