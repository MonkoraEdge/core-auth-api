using System.IdentityModel.Tokens.Jwt;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;

namespace MonkoraEdge.Core.Auth.API.Middleware;

/// <summary>
/// Scoped implementation of <see cref="ITenantContext"/>. A new instance is created per
/// request and populated once by <see cref="TenantContextMiddleware"/>.
/// </summary>
public sealed class RequestTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid? tenantId) => TenantId = tenantId;
}

/// <summary>
/// Resolves the tenant for each request and sets it on <see cref="ITenantContext"/>.
///
/// Resolution order (first match wins):
///   1. JWT access token claim "tid" — present when the bearer token was issued for a specific tenant.
///   2. X-Tenant-Id request header — used by machine-to-machine / API-key callers that don't carry a JWT.
///
/// Missing tenant context is NOT an error — single-tenant deployments and public endpoints
/// (discovery, JWKS, health) operate without a tenant ID. Downstream services that
/// require a tenant should throw explicitly when <see cref="ITenantContext.TenantId"/> is null.
/// </summary>
public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        Guid? tenantId = null;

        // 1. JWT "tid" claim — most authoritative source; embedded at token issuance time.
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            var rawToken = authHeader["Bearer ".Length..].Trim();
            // Read-only parse — no signature validation (already handled by JWT Bearer middleware).
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(rawToken))
            {
                var jwt = handler.ReadJwtToken(rawToken);
                var tid = jwt.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
                if (!string.IsNullOrEmpty(tid) && Guid.TryParse(tid, out var parsedTid))
                    tenantId = parsedTid;
            }
        }

        // 2. X-Tenant-Id header fallback — for M2M clients or pre-authentication requests.
        if (tenantId == null)
        {
            var header = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(header) && Guid.TryParse(header, out var headerTid))
                tenantId = headerTid;
        }

        tenantContext.SetTenant(tenantId);

        await _next(context);
    }
}
