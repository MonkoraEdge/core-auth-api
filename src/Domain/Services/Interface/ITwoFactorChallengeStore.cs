namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Short-lived store for pending 2FA login challenges.
/// Maps an opaque token (returned to the client after a password-correct + 2FA-required response)
/// to the authenticated user's ID. Consuming a token removes it — tokens are single-use.
/// Async so implementations can use distributed stores (Redis) without sync-over-async.
/// </summary>
public interface ITwoFactorChallengeStore
{
    /// <summary>Persist a token → userId binding for <paramref name="expiry"/>.</summary>
    Task StoreAsync(string token, Guid userId, TimeSpan expiry);

    /// <summary>
    /// Look up the userId for <paramref name="token"/> and remove it (single-use).
    /// Returns <c>null</c> if the token does not exist or has expired.
    /// </summary>
    Task<Guid?> ConsumeAsync(string token);
}
