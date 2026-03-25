using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

public partial class ConsoleController
{
    [Authorize]
    [HttpGet("api/[controller]/{id:guid}")]
    [ProducesResponseType(typeof(TenantResponse), 200)]
    public async Task<IActionResult> GetTenantByIdAsync([FromRoute] Guid id)
    {
        var result = await _tenantService.GetTenantByIdAsync(id);

        return Ok(result);
    }
}