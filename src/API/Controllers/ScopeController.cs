using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ScopeAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// OAuth scope management endpoints for defining API/resource scopes.
/// </summary>
[Route("scopes")]
[ApiController]
[Authorize]
public class ScopeController : ControllerBase
{
    private readonly IScopeService _scopeService;

    public ScopeController(IScopeService scopeService)
    {
        _scopeService = scopeService;
    }

    /// <summary>
    /// Resolve caller id for auditing scope changes.
    /// </summary>
    private string GetUserIdString()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return sub ?? "system";
    }

    /// <summary>
    /// List all scopes with optional active-only filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] bool activeOnly = true)
    {
        var result = await _scopeService.GetAllAsync(activeOnly);
        return Ok(result);
    }

    /// <summary>
    /// Get scope details by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _scopeService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Create a new scope definition.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ScopeCreateRequest request)
    {
        var result = await _scopeService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Update a scope definition.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ScopeUpdateRequest request)
    {
        var result = await _scopeService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Delete a scope. Domain rules protect built-in/system scopes.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _scopeService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }
}
