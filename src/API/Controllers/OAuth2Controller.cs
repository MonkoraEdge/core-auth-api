using System.Text;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
        var userId = GetAuthenticatedUserId();

        var validation = await _oauth2Service.ValidateAuthorizeRequestAsync(request, userId ?? Guid.Empty);
        if (!validation.IsValid)
        {
            if (!string.IsNullOrEmpty(request.RedirectUri) && !string.IsNullOrEmpty(request.State))
                return Redirect($"{request.RedirectUri}?error={validation.Error}&error_description={Uri.EscapeDataString(validation.ErrorDescription ?? "")}&state={request.State}");
            return BadRequest(new { error = validation.Error, error_description = validation.ErrorDescription });
        }

        if (validation.RequiresLogin)
        {
            // Return the client info so the frontend can show a login form
            return Ok(new
            {
                requires_login = true,
                client = validation.Client,
                requested_scopes = validation.RequestedScopes
            });
        }

        if (validation.RequiresConsent)
        {
            return Ok(new
            {
                requires_consent = true,
                client = validation.Client,
                requested_scopes = validation.RequestedScopes
            });
        }

        // Authenticated and consented — issue code
        var code = await _oauth2Service.IssueAuthorizationCodeAsync(request, userId!.Value, false);
        var redirectUrl = BuildRedirectUrl(request.RedirectUri!, code, request.State);
        return Redirect(redirectUrl);
    }

    /// <summary>Submit consent and receive authorization code</summary>
    [HttpPost("authorize/consent")]
    [Authorize]
    public async Task<IActionResult> SubmitConsent([FromBody] ConsentSubmitRequest request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null) return Unauthorized();

        if (!request.Approved)
        {
            return Redirect($"{request.RedirectUri}?error=access_denied&error_description=User+denied+access&state={request.State}");
        }

        var authorizeReq = new AuthorizeRequest
        {
            ClientId = request.ClientId,
            ResponseType = "code",
            RedirectUri = request.RedirectUri,
            Scope = request.Scope,
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Nonce = request.Nonce
        };

        var code = await _oauth2Service.IssueAuthorizationCodeAsync(authorizeReq, userId.Value, request.RememberConsent);
        return Ok(new { code, state = request.State, redirect_uri = request.RedirectUri });
    }

    /// <summary>Token endpoint — exchange authorization code, client credentials, or refresh token for tokens</summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> Token([FromForm] TokenFormRequest formRequest, [FromBody] TokenRequest? jsonRequest)
    {
        var request = jsonRequest ?? MapFormToTokenRequest(formRequest);
        ExtractClientCredentials(out var clientId, out var clientSecret, request);

        TokenResponse response = request.GrantType?.ToLower() switch
        {
            "authorization_code" => await _oauth2Service.ExchangeAuthorizationCodeAsync(
                request, clientId, clientSecret, GetIpAddress(), GetUserAgent()),
            "client_credentials" => await _oauth2Service.ClientCredentialsGrantAsync(
                request, clientId!, clientSecret, GetIpAddress(), GetUserAgent()),
            "refresh_token" => await _oauth2Service.RefreshTokenGrantAsync(
                request, clientId, clientSecret, GetIpAddress(), GetUserAgent()),
            _ => throw new ArgumentException($"Unsupported grant_type: {request.GrantType}")
        };

        return Ok(response);
    }

    /// <summary>Token revocation endpoint (RFC 7009)</summary>
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    public async Task<IActionResult> Revoke([FromForm] RevocationFormRequest formRequest, [FromBody] RevocationRequest? jsonRequest)
    {
        var request = jsonRequest ?? new RevocationRequest
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
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    public async Task<IActionResult> Introspect([FromForm] IntrospectFormRequest formRequest, [FromBody] IntrospectRequest? jsonRequest)
    {
        var request = jsonRequest ?? new IntrospectRequest
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
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
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

    private static string BuildRedirectUrl(string redirectUri, string code, string? state)
    {
        var url = $"{redirectUri}?code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state)) url += $"&state={Uri.EscapeDataString(state)}";
        return url;
    }

    private static TokenRequest MapFormToTokenRequest(TokenFormRequest f) => new()
    {
        GrantType = f.GrantType,
        Code = f.Code,
        RedirectUri = f.RedirectUri,
        CodeVerifier = f.CodeVerifier,
        RefreshToken = f.RefreshToken,
        Username = f.Username,
        Password = f.Password,
        Scope = f.Scope,
        ClientId = f.ClientId,
        ClientSecret = f.ClientSecret
    };
}
