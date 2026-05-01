using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;

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
            throw new DomainException("auth", MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate.ErrorCodeType.UNAUTHORIZED, "Invalid or missing user identity claim.");
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

    /// <summary>
    /// Resolve the session id from the <c>sid</c> JWT claim (OIDC Session Management).
    /// Returns <c>null</c> when the bearer token carries no session claim.
    /// </summary>
    protected Guid? GetSessionId()
    {
        var sid = User.FindFirstValue("sid");
        return Guid.TryParse(sid, out var id) ? id : null;
    }

    // ─── Request-context helpers ───────────────────────────────────────────────

    /// <summary>
    /// Resolve the caller IP address from the TCP connection's remote endpoint.
    /// NOTE: Deploy behind a reverse proxy that sets RemoteIpAddress correctly
    /// (e.g., Nginx with proxy_protocol, or Kestrel with UseForwardedHeaders).
    /// X-Forwarded-For is intentionally NOT used here — it is untrusted user input
    /// and must never be used for security decisions (rate limiting, audit logs).
    /// </summary>
    protected string GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>
    /// Resolve the caller User-Agent string for security telemetry.
    /// Truncated to 512 characters to prevent oversized values from reaching the DB.
    /// </summary>
    protected string? GetUserAgent()
    {
        var ua = Request.Headers.UserAgent.ToString();
        return string.IsNullOrEmpty(ua) ? null : (ua.Length > 512 ? ua[..512] : ua);
    }
}
