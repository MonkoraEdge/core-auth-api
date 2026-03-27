using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>Permission management endpoints</summary>
/// <summary>
/// Permission management endpoints for permission catalog CRUD operations.
/// </summary>
[Route("permissions")]
[ApiController]
[Authorize]
public class PermissionController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    private string GetUserIdString()
        /// <summary>
        /// Resolve caller id for auditing permission changes.
        /// </summary>
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return sub ?? "system";
    }

    /// <summary>List all permissions with optional filtering</summary>
        /// <summary>
        /// List permissions with tenant and active filters.
        /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Guid? tenantId, [FromQuery] bool activeOnly = true)
    {
        var result = await _permissionService.GetListAsync(tenantId, activeOnly);
        return Ok(result);
    }

    /// <summary>List permissions for a specific tenant</summary>
        /// <summary>
        /// List permissions that belong to a specific tenant.
        /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId)
    {
        var result = await _permissionService.GetListAsync(tenantId);
        return Ok(result);
    }

    /// <summary>Get a single permission by ID</summary>
        /// <summary>
        /// Get a single permission by identifier.
        /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _permissionService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Create a new permission</summary>
        /// <summary>
        /// Create a new permission entry.
        /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PermissionCreateRequest request)
    {
        var result = await _permissionService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Update an existing permission</summary>
        /// <summary>
        /// Update an existing permission entry.
        /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PermissionUpdateRequest request)
    {
        var result = await _permissionService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Delete a permission</summary>
        /// <summary>
        /// Delete a permission entry.
        /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _permissionService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }
}
