using System.Security.Claims;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// User administration endpoints for querying, provisioning, lifecycle status, and role assignments.
/// </summary>
[Route("users")]
[ApiController]
[Authorize]
public class UserController : MonkoraControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// List users using datasource filters (search, status, tenant, paging, sorting).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] UserDataSourceRequest request)
    {
        var result = await _userService.GetListAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed profile for a single user by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Get all users associated with a specific tenant.
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId)
    {
        var result = await _userService.GetByTenantIdAsync(tenantId);
        return Ok(result);
    }

    /// <summary>
    /// Create a new user account under optional tenant context.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request)
    {
        var result = await _userService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Update mutable user profile fields and account flags.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateRequest request)
    {
        var result = await _userService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Soft-delete a user record.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _userService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Mark a user account as active and eligible for authentication.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _userService.ActivateAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Mark a user account as inactive to block new authentications.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _userService.DeactivateAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Assign one or more roles to target user.
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRoleRequest request)
    {
        var result = await _userService.AssignRolesAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Remove one or more roles from target user.
    /// </summary>
    [HttpDelete("{id:guid}/roles")]
    public async Task<IActionResult> RemoveRoles(Guid id, [FromBody] AssignRoleRequest request)
    {
        var result = await _userService.RemoveRolesAsync(id, request.RoleIds, GetUserIdString());
        return Ok(result);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

}
