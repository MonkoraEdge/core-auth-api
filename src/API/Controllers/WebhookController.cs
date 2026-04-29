using System.Security.Cryptography;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.WebhookAggregate.Interfaces;
using DomainIUnitOfWork = MonkoraEdge.Core.Auth.Domain.Services.Interface.IUnitOfWork;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// Webhook endpoint management: register, list, and delete webhook subscriptions per client.
/// </summary>
[Route("webhooks")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class WebhookController : MonkoraControllerBase
{
    private readonly IWebhookEndpointRepository _endpointRepo;
    private readonly IWebhookDeliveryLogRepository _logRepo;
    private readonly DomainIUnitOfWork _unitOfWork;

    public WebhookController(
        IWebhookEndpointRepository endpointRepo,
        IWebhookDeliveryLogRepository logRepo,
        DomainIUnitOfWork unitOfWork)
    {
        _endpointRepo = endpointRepo;
        _logRepo      = logRepo;
        _unitOfWork   = unitOfWork;
    }

    /// <summary>List all webhook endpoints registered for a client.</summary>
    [HttpGet("clients/{clientId:guid}")]
    public async Task<IActionResult> GetByClient(Guid clientId, CancellationToken ct)
    {
        var endpoints = await _endpointRepo.GetByClientIdAsync(clientId, ct);
        // Never expose the signing secret in list responses.
        return Ok(endpoints.Select(e => new
        {
            e.Id, e.Url, e.Events, e.IsActive, e.CreatedAt
        }));
    }

    /// <summary>Register a new webhook endpoint for a client.</summary>
    [HttpPost("clients/{clientId:guid}")]
    public async Task<IActionResult> Create(Guid clientId, [FromBody] WebhookEndpointRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return BadRequest(new { error = "Invalid or missing URL." });

        if (string.IsNullOrWhiteSpace(request.Events))
            return BadRequest(new { error = "events is required (comma-separated event types or '*')." });

        // Generate a cryptographically random signing secret.
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var endpoint = new WebhookEndpoint
        {
            ClientId = clientId,
            Url      = request.Url,
            Events   = request.Events,
            Secret   = secret,
            IsActive = true
        };

        _endpointRepo.Insert(endpoint);
        await _unitOfWork.SaveChangesAsync();

        // Return the secret ONCE — the caller must store it; we never expose it again.
        return Created($"/webhooks/{endpoint.Id}", new
        {
            endpoint.Id,
            endpoint.Url,
            endpoint.Events,
            endpoint.IsActive,
            Secret = secret
        });
    }

    /// <summary>Activate or deactivate a webhook endpoint.</summary>
    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        var endpoint = await _endpointRepo.GetAsync(id, ct);
        if (endpoint is null) return NotFound();

        endpoint.IsActive = request.IsActive;
        _endpointRepo.Update(endpoint);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Delete (soft-delete) a webhook endpoint.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var endpoint = await _endpointRepo.GetAsync(id, ct);
        if (endpoint is null) return NotFound();

        endpoint.IsDeleted = true;
        endpoint.DeletedAt = DateTime.UtcNow;
        endpoint.DeletedBy = GetAuthenticatedUserId()?.ToString();
        endpoint.IsActive  = false;
        _endpointRepo.Update(endpoint);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Retrieve recent delivery logs for a webhook endpoint.</summary>
    [HttpGet("{id:guid}/deliveries")]
    public async Task<IActionResult> GetDeliveries(Guid id, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var logs = await _logRepo.GetByEndpointAsync(id, limit, ct);
        return Ok(logs);
    }
}

public sealed class WebhookEndpointRequest
{
    public string Url    { get; set; } = string.Empty;
    /// <summary>Comma-separated event types or "*" for all events.</summary>
    public string Events { get; set; } = string.Empty;
}

public sealed class SetActiveRequest
{
    public bool IsActive { get; set; }
}
