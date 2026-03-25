using System.Security.Claims;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>User management endpoints</summary>
[Route("users")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>List users with optional filtering and paging</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] UserDataSourceRequest request)
    {
        var result = await _userService.GetListAsync(request);
        return Ok(result);
    }

    /// <summary>Get a single user by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Get all users for a specific tenant</summary>
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId)
    {
        var result = await _userService.GetByTenantIdAsync(tenantId);
        return Ok(result);
    }

    /// <summary>Create a new user</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request)
    {
        var result = await _userService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Update an existing user</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateRequest request)
    {
        var result = await _userService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Delete a user (soft-delete)</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _userService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Activate a user account</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _userService.ActivateAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Deactivate a user account</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _userService.DeactivateAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Assign roles to a user</summary>
    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRoleRequest request)
    {
        var result = await _userService.AssignRolesAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>Remove roles from a user</summary>
    [HttpDelete("{id:guid}/roles")]
    public async Task<IActionResult> RemoveRoles(Guid id, [FromBody] AssignRoleRequest request)
    {
        var result = await _userService.RemoveRolesAsync(id, request.RoleIds, GetUserIdString());
        return Ok(result);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var id)) throw new UnauthorizedAccessException("Invalid user token.");
        return id;
    }

    private string GetUserIdString() => GetUserId().ToString();
}
