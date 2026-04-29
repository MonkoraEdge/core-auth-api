using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// Legal agreement and compliance document management (Terms of Service, Privacy Policy, PDPA).
/// </summary>
[Route("agreements")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class AgreementController : MonkoraControllerBase
{
    private readonly IAgreementService _agreementService;

    public AgreementController(IAgreementService agreementService)
    {
        _agreementService = agreementService ?? throw new ArgumentNullException(nameof(agreementService));
    }

    /// <summary>
    /// List agreements with optional tenant and active-only filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Guid? tenantId, [FromQuery] bool activeOnly = true)
    {
        var result = await _agreementService.GetListAsync(tenantId, activeOnly);
        return Ok(result);
    }

    /// <summary>
    /// Get agreement by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AgreementResponse), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _agreementService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// List active agreements for a specific tenant.
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId)
    {
        var result = await _agreementService.GetListAsync(tenantId);
        return Ok(result);
    }

    /// <summary>
    /// Create a new legal agreement document.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AgreementCreateRequest request)
    {
        var result = await _agreementService.CreateAsync(request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Update agreement content, version, or effective period.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AgreementUpdateRequest request)
    {
        var result = await _agreementService.UpdateAsync(id, request, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Soft-delete an agreement document. Acceptance audit trail is preserved.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _agreementService.DeleteAsync(id, GetUserIdString());
        return Ok(result);
    }

    /// <summary>
    /// Record the authenticated user's acceptance of a legal agreement. Idempotent.
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AgreementAcceptRequest request)
    {
        var result = await _agreementService.AcceptAsync(id, GetUserId(), request, GetIpAddress(), GetUserAgent());
        return Ok(result);
    }
}
