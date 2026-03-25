using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>OpenID Connect discovery endpoints</summary>
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

    /// <summary>OpenID Connect Discovery Document (RFC 8414)</summary>
    [HttpGet("/.well-known/openid-configuration")]
    [Produces("application/json")]
    public IActionResult GetOpenIdConfiguration()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(_oauth2Service.GetOpenIdConfiguration(baseUrl));
    }

    /// <summary>OAuth 2.0 Authorization Server Metadata (RFC 8414)</summary>
    [HttpGet("/.well-known/oauth-authorization-server")]
    [Produces("application/json")]
    public IActionResult GetAuthorizationServerMetadata()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(_oauth2Service.GetAuthorizationServerMetadata(baseUrl));
    }

    /// <summary>JSON Web Key Set (RFC 7517)</summary>
    [HttpGet("/.well-known/jwks.json")]
    [Produces("application/json")]
    public IActionResult GetJwks()
    {
        return Ok(_tokenService.GetJwks());
    }
}
