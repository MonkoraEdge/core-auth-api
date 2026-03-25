using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.Auth.Infrastructure.Configurations;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>OpenID Connect discovery endpoints</summary>
[ApiController]
public class WellKnownController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WellKnownController(ITokenService tokenService, IHttpContextAccessor httpContextAccessor)
    {
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>OpenID Connect Discovery Document (RFC 8414)</summary>
    [HttpGet("/.well-known/openid-configuration")]
    [Produces("application/json")]
    public IActionResult GetOpenIdConfiguration()
    {
        var issuer = _tokenService.GetIssuer();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return Ok(new OpenIdConfigurationResponse
        {
            Issuer = issuer,
            AuthorizationEndpoint = $"{baseUrl}/oauth2/authorize",
            TokenEndpoint = $"{baseUrl}/oauth2/token",
            UserInfoEndpoint = $"{baseUrl}/oauth2/userinfo",
            JwksUri = $"{baseUrl}/.well-known/jwks.json",
            RevocationEndpoint = $"{baseUrl}/oauth2/revoke",
            IntrospectionEndpoint = $"{baseUrl}/oauth2/introspect",
            ResponseTypesSupported = new[] { "code" },
            // OAuth 2.1: password grant and plain PKCE are removed
            GrantTypesSupported = new[] { "authorization_code", "client_credentials", "refresh_token" },
            SubjectTypesSupported = new[] { "public" },
            IdTokenSigningAlgValuesSupported = new[] { "RS256" },
            ScopesSupported = new[] { "openid", "profile", "email", "phone", "address", "offline_access" },
            ClaimsSupported = new[] { "sub", "iss", "iat", "exp", "aud", "client_id", "scope", "email", "name", "phone_number" },
            CodeChallengeMethodsSupported = new[] { "S256" },
            TokenEndpointAuthMethodsSupported = new[] { "client_secret_basic", "client_secret_post" },
            EndSessionEndpoint = $"{baseUrl}/oauth2/end-session"
        });
    }

    /// <summary>JSON Web Key Set (RFC 7517)</summary>
    [HttpGet("/.well-known/jwks.json")]
    [Produces("application/json")]
    public IActionResult GetJwks()
    {
        return Ok(_tokenService.GetJwks());
    }
}
