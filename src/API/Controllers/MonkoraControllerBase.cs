using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// Shared controller base that provides claim and request-context helpers used across
/// every controller in this project. Keeps individual controllers thin and eliminates
/// copy-pasted boilerplate.
/// </summary>
public abstract class MonkoraControllerBase : ControllerBase
{
    // ─── Identity helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolve the authenticated user id from the JWT sub/NameIdentifier claim.
    /// Throws <see cref="UnauthorizedAccessException"/> when the token is missing or malformed.
    /// </summary>
    protected Guid GetUserId()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var id))
            throw new UnauthorizedAccessException("Invalid user token.");
        return id;
    }

    /// <summary>
    /// Resolve the authenticated user id as a string for auditing metadata fields.
    /// Returns <c>"system"</c> when no valid claim is present (e.g. service-level calls).
    /// </summary>
    protected string GetUserIdString()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return sub ?? "system";
    }

    /// <summary>
    /// Resolve the authenticated user id when authentication is optional (e.g. /authorize).
    /// Returns <c>null</c> when the request is anonymous.
    /// </summary>
    protected Guid? GetAuthenticatedUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    // ─── Request-context helpers ───────────────────────────────────────────────

    /// <summary>
    /// Resolve the caller IP address, respecting the <c>X-Forwarded-For</c> reverse-proxy header.
    /// </summary>
    protected string GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString()
        ?? Request.Headers["X-Forwarded-For"].FirstOrDefault()
        ?? "unknown";

    /// <summary>
    /// Resolve the caller User-Agent string for security telemetry.
    /// </summary>
    protected string? GetUserAgent() => Request.Headers.UserAgent.ToString();
}
