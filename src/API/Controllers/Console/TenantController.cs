using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

public partial class ConsoleController
{
    /// <summary>
    /// Get tenant details by identifier.
    /// </summary>
    [Authorize]
    [HttpGet("tenants/{id:guid}")]
    [ProducesResponseType(typeof(TenantResponse), 200)]
    public async Task<IActionResult> GetTenantByIdAsync([FromRoute] Guid id)
    {
        var result = await _tenantService.GetTenantByIdAsync(id);
        return Ok(result);
    }
}