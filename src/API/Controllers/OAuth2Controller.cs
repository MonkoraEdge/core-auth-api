using System.Text;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// OAuth2/OIDC protocol endpoints exposed by this authorization server.
/// Covers authorize, token issuance, revocation, introspection, userinfo, and logout session handling.
/// </summary>
[Route("oauth2")]
[ApiController]
public class OAuth2Controller : MonkoraControllerBase
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
    [HttpGet("/authorize")]
    [HttpPost("/authorize")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequest request)
    {
        try
        {
            var response = await _oauth2Service.ProcessAuthorizeRequestAsync(request, GetAuthenticatedUserId());
            return ToActionResult(response);
        }
        catch (DomainException ex)
        {
            return OAuthError("invalid_request", ex.ErrorMessage, StatusCodes.Status400BadRequest);
        }
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
    [HttpPost("/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Token([FromForm] TokenFormRequest formRequest)
    {
        if (string.IsNullOrWhiteSpace(formRequest.GrantType))
            return OAuthError("invalid_request", "grant_type is required.", StatusCodes.Status400BadRequest);

        var request = MapFormToTokenRequest(formRequest);
        ExtractClientCredentials(out var clientId, out var clientSecret, request);

        try
        {
            var response = await _oauth2Service.ProcessTokenRequestAsync(
                request, clientId, clientSecret, GetIpAddress(), GetUserAgent());

            ApplyNoStoreHeaders();
            return Ok(response);
        }
        catch (DomainException ex)
        {
            return MapTokenDomainException(ex);
        }
    }

    /// <summary>
    /// Token revocation endpoint (RFC 7009).
    /// Revokes caller-owned token and always responds HTTP 200 per RFC behavior.
    /// </summary>
    [HttpPost("revoke")]
    [HttpPost("/revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Revoke([FromForm] RevocationFormRequest formRequest)
    {
        if (string.IsNullOrWhiteSpace(formRequest.Token))
            return OAuthError("invalid_request", "token is required.", StatusCodes.Status400BadRequest);

        if (!string.IsNullOrWhiteSpace(formRequest.TokenTypeHint)
            && !IsSupportedTokenTypeHint(formRequest.TokenTypeHint))
        {
            return OAuthError("unsupported_token_type", "token_type_hint must be access_token or refresh_token.",
                StatusCodes.Status400BadRequest);
        }

        var request = new RevocationRequest
        {
            Token = formRequest.Token,
            TokenTypeHint = formRequest.TokenTypeHint
        };
        ExtractClientCredentials(out var clientId, out var clientSecret, null);

        try
        {
            await _oauth2Service.RevokeAsync(request, clientId ?? string.Empty, clientSecret);
            // RFC 7009 §2.2 + RFC 6749 §5.1: token endpoint responses MUST include no-store.
            ApplyNoStoreHeaders();
            return Ok(); // RFC 7009: always 200 for validly authenticated requests
        }
        catch (DomainException ex)
        {
            if (IsInvalidClientError(ex))
                return OAuthError("invalid_client", "Client authentication failed.", StatusCodes.Status401Unauthorized,
                    "Basic realm=\"oauth2/revoke\", error=\"invalid_client\"");

            return OAuthError("invalid_request", ex.ErrorMessage, StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Token introspection endpoint (RFC 7662).
    /// Returns active/inactive token metadata for authorized clients.
    /// </summary>
    [HttpPost("introspect")]
    [HttpPost("/introspect")]
    [Consumes("application/x-www-form-urlencoded")]
    [EnableRateLimiting("default")]
    public async Task<IActionResult> Introspect([FromForm] IntrospectFormRequest formRequest)
    {
        if (string.IsNullOrWhiteSpace(formRequest.Token))
            return OAuthError("invalid_request", "token is required.", StatusCodes.Status400BadRequest);

        if (!string.IsNullOrWhiteSpace(formRequest.TokenTypeHint)
            && !IsSupportedTokenTypeHint(formRequest.TokenTypeHint))
        {
            return OAuthError("invalid_request", "token_type_hint must be access_token or refresh_token.",
                StatusCodes.Status400BadRequest);
        }

        var request = new IntrospectRequest
        {
            Token = formRequest.Token,
            TokenTypeHint = formRequest.TokenTypeHint
        };
        ExtractClientCredentials(out var clientId, out var clientSecret, null);

        try
        {
            var result = await _oauth2Service.IntrospectAsync(request, clientId ?? string.Empty, clientSecret);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            if (IsInvalidClientError(ex))
                return OAuthError("invalid_client", "Client authentication failed.", StatusCodes.Status401Unauthorized,
                    "Basic realm=\"oauth2/introspect\", error=\"invalid_client\"");

            return OAuthError("invalid_request", ex.ErrorMessage, StatusCodes.Status400BadRequest);
        }
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
        if (string.IsNullOrEmpty(token))
        {
            Response.Headers[HeaderNames.WWWAuthenticate] = "Bearer realm=\"oauth2/userinfo\"";
            return Unauthorized();
        }

        try
        {
            var result = await _oauth2Service.GetUserInfoAsync(token);
            return Ok(result);
        }
        catch (DomainException ex)
            when (ex.ErrorCode == ErrorCodeType.SCOPE_NOT_ALLOWED || ex.ErrorCode == ErrorCodeType.INVALID_SCOPE)
        {
            // Sanitize error_description — strip any characters that break the header field-value
            // per RFC 7235 §2.1 (quoted-string must not contain bare CR/LF or unescaped double-quotes).
            Response.Headers[HeaderNames.WWWAuthenticate] =
                $"Bearer realm=\"oauth2/userinfo\", error=\"insufficient_scope\", error_description=\"{SanitizeHeaderValue(ex.ErrorMessage)}\"";
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (DomainException ex)
        {
            Response.Headers[HeaderNames.WWWAuthenticate] =
                $"Bearer realm=\"oauth2/userinfo\", error=\"invalid_token\", error_description=\"{SanitizeHeaderValue(ex.ErrorMessage)}\"";
            return Unauthorized();
        }
    }

    /// <summary>
    /// End session endpoint for RP-initiated logout.
    /// Revokes active sessions and redirects to post_logout_redirect_uri only when it
    /// exactly matches a URI registered by the client identified via id_token_hint or client_id.
    /// </summary>
    [HttpGet("end-session")]
    [HttpPost("end-session")]
    public async Task<IActionResult> EndSession(
        [FromQuery] string? id_token_hint,
        [FromQuery] string? post_logout_redirect_uri,
        [FromQuery] string? client_id)
    {
        var userId = GetAuthenticatedUserId();
        var validatedRedirectUri = await _oauth2Service.EndSessionAsync(
            userId, id_token_hint, post_logout_redirect_uri, client_id);

        if (!string.IsNullOrEmpty(validatedRedirectUri))
            return Redirect(validatedRedirectUri);

        return Ok(new { message = "Session ended." });
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────



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

    private IActionResult MapTokenDomainException(DomainException ex)
    {
        if (IsInvalidClientError(ex))
        {
            return OAuthError("invalid_client", "Client authentication failed.", StatusCodes.Status401Unauthorized,
                "Basic realm=\"oauth2/token\", error=\"invalid_client\"");
        }

        if (ex.ErrorCode == ErrorCodeType.UNAUTHORIZED_CLIENT)
            return OAuthError("unauthorized_client", ex.ErrorMessage, StatusCodes.Status400BadRequest);

        if (ex.ErrorCode == ErrorCodeType.UNSUPPORTED_GRANT_TYPE)
            return OAuthError("unsupported_grant_type", ex.ErrorMessage, StatusCodes.Status400BadRequest);

        if (ex.ErrorCode == ErrorCodeType.INVALID_SCOPE || ex.ErrorCode == ErrorCodeType.SCOPE_NOT_ALLOWED)
            return OAuthError("invalid_scope", ex.ErrorMessage, StatusCodes.Status400BadRequest);

        if (ex.ErrorCode == ErrorCodeType.INVALID_GRANT
            || ex.ErrorCode == ErrorCodeType.TOKEN_EXPIRED
            || ex.ErrorCode == ErrorCodeType.TOKEN_REVOKED
            || ex.ErrorCode == ErrorCodeType.INVALID_TOKEN
            || ex.ErrorCode == ErrorCodeType.INVALID_PKCE_CODE_VERIFIER
            || ex.ErrorCode == ErrorCodeType.INVALID_REDIRECT_URI)
        {
            return OAuthError("invalid_grant", ex.ErrorMessage, StatusCodes.Status400BadRequest);
        }

        if (ex.ErrorCode == ErrorCodeType.INVALID_REQUEST)
            return OAuthError("invalid_request", ex.ErrorMessage, StatusCodes.Status400BadRequest);

        var message = ex.ErrorMessage.ToLowerInvariant();
        if (message.Contains("scope"))
            return OAuthError("invalid_scope", ex.ErrorMessage, StatusCodes.Status400BadRequest);
        if (message.Contains("grant"))
            return OAuthError("invalid_grant", ex.ErrorMessage, StatusCodes.Status400BadRequest);

        return OAuthError("invalid_request", ex.ErrorMessage, StatusCodes.Status400BadRequest);
    }

    private static bool IsSupportedTokenTypeHint(string tokenTypeHint)
        => tokenTypeHint.Equals("access_token", StringComparison.OrdinalIgnoreCase)
           || tokenTypeHint.Equals("refresh_token", StringComparison.OrdinalIgnoreCase);

    private static bool IsInvalidClientError(DomainException ex)
        => ex.ErrorCode == ErrorCodeType.INVALID_CLIENT
           || ex.ErrorCode == ErrorCodeType.INVALID_CLIENT_SECRET;

    /// <summary>
    /// Strip characters that are illegal inside an HTTP quoted-string header value (RFC 7235 §2.1).
    /// Prevents header injection via error messages that travel into WWW-Authenticate.
    /// </summary>
    private static string SanitizeHeaderValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Remove CR, LF (header splitting) and bare double-quotes (quoted-string delimiter).
        return value.Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace("\"", "'");
    }

    private void ApplyNoStoreHeaders()
    {
        Response.Headers[HeaderNames.CacheControl] = "no-store";
        Response.Headers[HeaderNames.Pragma] = "no-cache";
    }

    private IActionResult OAuthError(string error, string? description, int statusCode, string? wwwAuthenticate = null)
    {
        ApplyNoStoreHeaders();

        if (!string.IsNullOrWhiteSpace(wwwAuthenticate))
            Response.Headers[HeaderNames.WWWAuthenticate] = wwwAuthenticate;

        return StatusCode(statusCode, new
        {
            error,
            error_description = description
        });
    }
}
