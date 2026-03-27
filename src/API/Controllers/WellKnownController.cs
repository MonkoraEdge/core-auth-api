using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// Discovery endpoints for OAuth2/OIDC clients to bootstrap server metadata and keys.
/// </summary>
[ApiController]
public class WellKnownController : ControllerBase
{
    private readonly IOAuth2Service _oauth2Service;
    private readonly ITokenService _tokenService;

    public WellKnownController(IOAuth2Service oauth2Service, ITokenService tokenService)
    {
        _oauth2Service = oauth2Service;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Return OIDC discovery document used by clients/libraries for dynamic configuration.
    /// </summary>
    [HttpGet("/.well-known/openid-configuration")]
    [Produces("application/json")]
    public IActionResult GetOpenIdConfiguration()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(_oauth2Service.GetOpenIdConfiguration(baseUrl));
    }

    /// <summary>
    /// Return OAuth authorization server metadata (RFC 8414).
    /// </summary>
    [HttpGet("/.well-known/oauth-authorization-server")]
    [Produces("application/json")]
    public IActionResult GetAuthorizationServerMetadata()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(_oauth2Service.GetAuthorizationServerMetadata(baseUrl));
    }

    /// <summary>
    /// Return public signing keys (JWKS) so token consumers can validate JWT signatures.
    /// </summary>
    [HttpGet("/.well-known/jwks.json")]
    [Produces("application/json")]
    public IActionResult GetJwks()
    {
        return Ok(_tokenService.GetJwks());
    }
}
