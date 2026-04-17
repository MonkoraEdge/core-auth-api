using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// External Identity Provider management (Google, Facebook, Apple, custom OIDC/OAuth2).
/// </summary>
[Route("providers")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class ProviderController : MonkoraControllerBase
{
    private readonly IProviderService _providerService;

    public ProviderController(IProviderService providerService)
    {
        _providerService = providerService ?? throw new ArgumentNullException(nameof(providerService));
    }

    /// <summary>
    /// List all identity providers with optional active-only filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] bool activeOnly = true)
    {
        var result = await _providerService.GetAllAsync(activeOnly);
        return Ok(result);
    }

    /// <summary>
    /// Get identity provider details by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProviderResponse), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _providerService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Register a new external identity provider.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProviderCreateRequest request)
    {
        var result = await _providerService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Update provider configuration (credentials, endpoints, active state).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProviderUpdateRequest request)
    {
        var result = await _providerService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Soft-delete an identity provider. Existing linked accounts are preserved.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _providerService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Activate a previously deactivated provider.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _providerService.UpdateAsync(id, new ProviderUpdateRequest { IsActive = true }, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Deactivate a provider. New social logins via this provider will be blocked.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _providerService.UpdateAsync(id, new ProviderUpdateRequest { IsActive = false }, GetUserIdString());
        return Ok(result);
    }
}
