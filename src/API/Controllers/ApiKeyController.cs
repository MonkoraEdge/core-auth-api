using System.Security.Claims;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ApiKeyAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// API key management endpoints for issuing, listing, revoking, and validating service keys.
/// </summary>
[Route("api-keys")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class ApiKeyController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeyController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>
    /// List API keys owned by current authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var result = await _apiKeyService.GetByUserIdAsync(GetUserId());
        return Ok(result);
    }

    /// <summary>
    /// List API keys associated with a specific OAuth client.
    /// </summary>
    [HttpGet("client/{clientId:guid}")]
    public async Task<IActionResult> GetByClient(Guid clientId)
    {
        var result = await _apiKeyService.GetByClientIdAsync(clientId);
        return Ok(result);
    }

    /// <summary>
    /// Get API key metadata by key identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _apiKeyService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Create a new API key and return plain-text value once at creation time.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ApiKeyCreateRequest request)
    {
        var result = await _apiKeyService.CreateAsync(request, GetUserId().ToString());
        return Ok(result);
    }

    /// <summary>
    /// Revoke an API key and prevent further authentication usage.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var result = await _apiKeyService.RevokeAsync(id, GetUserId().ToString());
        return Ok(result);
    }

    /// <summary>
    /// Validate API key string and return validation result for internal service-to-service use.
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate([FromBody] ApiKeyValidateRequest request)
    {
        var result = await _apiKeyService.ValidateAsync(request.ApiKey);
        return Ok(result);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve caller user id from token claims.
    /// </summary>
    private Guid GetUserId()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var id)) throw new UnauthorizedAccessException("Invalid user token.");
        return id;
    }
}

/// <summary>API key validation request</summary>
public class ApiKeyValidateRequest
{
    public string ApiKey { get; set; } = string.Empty;
}
