using MonkoraEdge.Core.Auth.Domain.AggregatesModel.RoleAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// Role management endpoints for tenant/global role catalog and role-permission mappings.
/// </summary>
[Route("roles")]
[ApiController]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Resolve caller id for auditing role changes.
    /// </summary>
    private string GetUserIdString()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return sub ?? "system";
    }

    /// <summary>
    /// List roles with optional tenant and active filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Guid? tenantId, [FromQuery] bool activeOnly = true)
    {
        var result = await _roleService.GetListAsync(tenantId, activeOnly);
        return Ok(result);
    }

    /// <summary>
    /// List all roles within a specific tenant scope.
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId)
    {
        var result = await _roleService.GetListAsync(tenantId);
        return Ok(result);
    }

    /// <summary>
    /// Get a single role by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _roleService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Create a new role definition.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleCreateRequest request)
    {
        var result = await _roleService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Update role metadata and status.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RoleUpdateRequest request)
    {
        var result = await _roleService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Delete a role definition.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roleService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Assign permission set to target role.
    /// </summary>
    [HttpPost("{id:guid}/permissions")]
    public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignPermissionRequest request)
    {
        var result = await _roleService.AssignPermissionsAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Remove permission set from target role.
    /// </summary>
    [HttpDelete("{id:guid}/permissions")]
    public async Task<IActionResult> RemovePermissions(Guid id, [FromBody] AssignPermissionRequest request)
    {
        var result = await _roleService.RemovePermissionsAsync(id, request.PermissionIds, GetUserIdString());
        return Ok(result);
    }
}
