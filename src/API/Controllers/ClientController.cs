using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ClientAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// OAuth client application management endpoints:
/// registration, updates, activation lifecycle, and secret rotation.
/// </summary>
[Route("clients")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class ClientController : MonkoraControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    /// <summary>
    /// List OAuth clients with optional tenant filter, paging, and search.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Guid? tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var result = await _clientService.GetListAsync(tenantId, page, pageSize, search);
        return Ok(result);
    }

    /// <summary>
    /// List OAuth clients that belong to a specific tenant.
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _clientService.GetListAsync(tenantId, page, pageSize, null);
        return Ok(result);
    }

    /// <summary>
    /// Get details for a single client by internal identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _clientService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Register a new OAuth client.
    /// For confidential clients, returns plain-text secret once at creation.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientCreateRequest request)
    {
        var result = await _clientService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Update OAuth client metadata, grant settings, and redirect/logout URIs.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClientUpdateRequest request)
    {
        var result = await _clientService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Delete an OAuth client.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _clientService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Rotate confidential client secret and invalidate previous secret immediately.
    /// </summary>
    [HttpPost("{id:guid}/rotate-secret")]
    public async Task<IActionResult> RotateSecret(Guid id)
    {
        var result = await _clientService.RotateSecretAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Activate client to allow protocol usage.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _clientService.ActivateAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Deactivate client to block protocol usage.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _clientService.DeactivateAsync(id);
        return Ok(result);
    }
}
