namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>Data stored per 2FA login challenge.</summary>
/// <param name="UserId">The authenticated user awaiting second factor.</param>
/// <param name="ClientId">The OAuth2 client_id string used during password login (may be null for direct logins).</param>
public record TwoFactorChallengeData(Guid UserId, string? ClientId);

/// <summary>
/// Short-lived store for pending 2FA login challenges.
/// Maps an opaque token (returned to the client after a password-correct + 2FA-required response)
/// to the authenticated user's ID and originating client. Consuming a token removes it — single-use.
/// Async so implementations can use distributed stores (Redis) without sync-over-async.
/// </summary>
public interface ITwoFactorChallengeStore
{
    /// <summary>Persist a token → (userId, clientId) binding for <paramref name="expiry"/>.</summary>
    Task StoreAsync(string token, Guid userId, string? clientId, TimeSpan expiry);

    /// <summary>
    /// Look up the challenge data for <paramref name="token"/> and remove it (single-use).
    /// Returns <c>null</c> if the token does not exist or has expired.
    /// </summary>
    Task<TwoFactorChallengeData?> ConsumeAsync(string token);
}
