namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Integration contract for the external Business Profile API (BP API).
/// Called after user registration or social login to ensure
/// a business profile record is kept in sync with every identity.
/// </summary>
public interface IBpApi
{
    /// <summary>
    /// Creates or updates the customer profile in the BP API keyed by identity userId.
    /// Idempotent — safe to call on every registration and social login.
    /// </summary>
    Task<BpCustomerResult?> EnsureCustomerAsync(
        Guid userId,
        string email,
        string? displayName,
        string? phone,
        string? locale = null,
        CancellationToken ct = default);

    /// <summary>
    /// Links a social provider identity to an existing business profile.
    /// Called after first-time social login so BP API can correlate provider IDs.
    /// </summary>
    Task LinkIdentityAsync(
        Guid userId,
        string providerCode,
        string externalUserId,
        string? externalEmail,
        CancellationToken ct = default);
}

public sealed class BpCustomerResult
{
    public string CustomerId { get; init; } = string.Empty;
    public bool IsNew { get; init; }
}
