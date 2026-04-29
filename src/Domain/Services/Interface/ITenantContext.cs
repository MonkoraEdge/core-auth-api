namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Provides the resolved tenant for the current request.
/// Populated by <c>TenantContextMiddleware</c> early in the pipeline.
/// Resolution order: JWT claim "tid" → X-Tenant-Id header.
/// Services and repositories consume this to scope data access to the correct tenant.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The resolved tenant ID for this request, or <c>null</c> when the server operates
    /// in single-tenant mode or when the request does not carry tenant information
    /// (e.g. /.well-known/openid-configuration).
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>Set by the middleware once the tenant has been resolved. Not for use outside of middleware.</summary>
    void SetTenant(Guid? tenantId);
}
