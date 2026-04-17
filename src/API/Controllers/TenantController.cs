using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// Tenant management endpoints for multi-tenant provisioning and lifecycle.
/// </summary>
[Route("tenants")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class TenantController : MonkoraControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantController(ITenantService tenantService)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
    }

    /// <summary>
    /// List tenants using datasource filters (keyword, isActive, paging).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] TenantDataSourceRequest request)
    {
        var result = await _tenantService.GetTenantsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get tenant details by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantResponse), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _tenantService.GetTenantByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Create a new tenant. TenantCode must be unique, uppercase alphanumeric.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TenantCreateRequest request)
    {
        var result = await _tenantService.CreateTenantAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Update tenant display name, active state, or settings.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TenantUpdateRequest request)
    {
        var result = await _tenantService.UpdateTenantAsync(id, request);
        return Ok(result);
    }

    /// <summary>
    /// Soft-delete a tenant. All child resources are preserved for audit.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _tenantService.DeleteTenantAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Re-activate a previously deactivated tenant.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _tenantService.UpdateTenantAsync(id, new TenantUpdateRequest { IsActive = true });
        return Ok(result);
    }

    /// <summary>
    /// Deactivate a tenant, blocking all child authentications.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _tenantService.UpdateTenantAsync(id, new TenantUpdateRequest { IsActive = false });
        return Ok(result);
    }
}
