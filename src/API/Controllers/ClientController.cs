using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ClientAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>OAuth2 client (application) management endpoints</summary>
[Route("clients")]
[ApiController]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    private string GetUserIdString()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return sub ?? "system";
    }

    /// <summary>List all OAuth2 clients with optional filtering</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Guid? tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var result = await _clientService.GetListAsync(tenantId, page, pageSize, search);
        return Ok(result);
    }

    /// <summary>List clients for a specific tenant</summary>
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _clientService.GetListAsync(tenantId, page, pageSize, null);
        return Ok(result);
    }

    /// <summary>Get a single client by its internal ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _clientService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Register a new OAuth2 client — returns the plain-text secret (shown once)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientCreateRequest request)
    {
        var result = await _clientService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Update an existing OAuth2 client</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClientUpdateRequest request)
    {
        var result = await _clientService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Delete an OAuth2 client</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _clientService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Rotate the client secret — invalidates old secret immediately</summary>
    [HttpPost("{id:guid}/rotate-secret")]
    public async Task<IActionResult> RotateSecret(Guid id)
    {
        var result = await _clientService.RotateSecretAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Activate a client</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _clientService.ActivateAsync(id);
        return Ok(result);
    }

    /// <summary>Deactivate a client</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _clientService.DeactivateAsync(id);
        return Ok(result);
    }
}
